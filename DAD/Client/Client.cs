using System.Runtime.Serialization;
using BankClient;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;

namespace Client
{
    static class BankClient
    {
        private static int id;
        private static Dictionary<string, BankClientService.BankClientServiceClient> servers = new Dictionary<string, BankClientService.BankClientServiceClient>();  
        [STAThread]
        static void Main(string[] args)
        {
            id = Int16.Parse(args[0]);
            Console.WriteLine("ID = " + id);
            bool keepRunnning = true;
            createChannels(args);
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            while (keepRunnning)
            {
                Console.WriteLine("Hi there, press 'D' to make a deposit, 'W' to make a withdrawal or 'R' to read your current balance.\r\n" +
                    "In case, you want to finish your activity press 'X'\r\n");
                switch (Console.ReadKey().Key)
                {
                    case ConsoleKey.D:
                        Console.WriteLine("\r\nHow much do you want to deposit ?\r\n");
                        double ammount = Convert.ToDouble(Console.ReadLine());
                        DepositRequest request = new DepositRequest{Ammount = ammount};
                        foreach (KeyValuePair<string, BankClientService.BankClientServiceClient> server in servers)
                        {
                            DepositReply reply = server.Value.Deposit(request);
                            if (reply.Balance != -1)
                                Console.WriteLine("Well Done! Your current balance is:" + reply.Balance.ToString() + "\r\n");
                            else continue;
                        }
                        break;
                    
                    case ConsoleKey.W:
                        Console.WriteLine("\r\nHow much do you want to withdrawal ?\r\n");
                        double ammount2 = Convert.ToDouble(Console.ReadLine());
                        foreach (KeyValuePair<string, BankClientService.BankClientServiceClient> server in servers)
                        {
                            WithdrawalRequest request2 = new WithdrawalRequest { Ammount = ammount2 };
                            WithdrawalReply reply2 = server.Value.Withdrawal(request2);
                            if (reply2.Success)
                            {
                                Console.WriteLine("Well Done! Your current balance is:" + reply2.Balance.ToString() + "\r\n");
                            }
                            else if (!reply2.Success && reply2.Balance != -1 )
                            {
                                Console.WriteLine("Ups, you are not rich enough! Your current balance is:" + reply2.Balance.ToString() + "\r\n");
                            }
                            else
                            {
                                continue;
                            }
                        }
                        break;
                    case ConsoleKey.R:
                        ReadBalanceRequest request3 = new ReadBalanceRequest { };
                        foreach (KeyValuePair<string, BankClientService.BankClientServiceClient> server in servers)
                        {
                            ReadBalanceReply reply3 = server.Value.ReadBalance(request3);
                            Console.WriteLine("\r\nYour current balance is:" + reply3.Balance.ToString() + "\r\n");
                        }
                        break;
                    case ConsoleKey.X:
                        keepRunnning = false;
                        break;
                }
            }
        }

        public static void createChannels(string[] args)
        {
            for(int i = 1; i < args.Length; i++)
            {
                GrpcChannel channel = GrpcChannel.ForAddress("http://localhost:" + args[i]);
                CallInvoker interceptingInvoker = channel.Intercept(new ClientInterceptor(id));
                BankClientService.BankClientServiceClient server = new BankClientService.BankClientServiceClient(interceptingInvoker);
                string host = "http://localhost:" + args[i];
                servers.Add(host, server);
            }
        }
}

    public class ClientInterceptor : Interceptor
    {
        private int Id;
        private int count;

        public ClientInterceptor(int id)
        {
            Id = id;
            count = 1;
        }

        public override TResponse BlockingUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
        {

            Metadata metadata = new Metadata ();
            metadata.Add("dad",Id.ToString() + " " + count.ToString());

            count++;
            if (metadata == null) { metadata = new Metadata(); }

            // create new context because original context is readonly
            ClientInterceptorContext<TRequest, TResponse> modifiedContext =
                new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host,
                    new CallOptions(metadata, context.Options.Deadline,
                        context.Options.CancellationToken, context.Options.WriteOptions,
                        context.Options.PropagationToken, context.Options.Credentials));
            //Console.Write("calling server...");
            TResponse response = base.BlockingUnaryCall(request, modifiedContext, continuation);
            return response;
        }
    }
}