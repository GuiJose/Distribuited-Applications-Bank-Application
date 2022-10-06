using BankClient;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;

namespace Client
{
    static class BankClient
    {
        private static Dictionary<string, BankClientService.BankClientServiceClient> servers = new Dictionary<string, BankClientService.BankClientServiceClient>();  
        [STAThread]
        static void Main(string[] args)
        {
            bool keepRunnning = true;
            createChannels(args[0]);
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
                        DepositReply reply = servers["http://localhost:10003"].Deposit(request);
                        Console.WriteLine("Well Done! Your current balance is:" + reply.Balance.ToString() + "\r\n");
                        break;
                    
                    case ConsoleKey.W:
                        Console.WriteLine("\r\nHow much do you want to withdrawal ?\r\n");
                        double ammount2 = Convert.ToDouble(Console.ReadLine());
                        WithdrawalRequest request2 = new WithdrawalRequest { Ammount = ammount2};
                        WithdrawalReply reply2 = servers["http://localhost:10003"].Withdrawal(request2);
                        if (reply2.Success)
                        {
                            Console.WriteLine("Well Done! Your current balance is:" + reply2.Balance.ToString() + "\r\n");
                        }
                        else
                        {
                            Console.WriteLine("Ups, you are not rich enough! Your current balance is:" + reply2.Balance.ToString() + "\r\n");
                        }
                        break;
                    case ConsoleKey.R:
                        ReadBalanceRequest request3 = new ReadBalanceRequest { };
                        ReadBalanceReply reply3 = servers["http://localhost:10003"].ReadBalance(request3);
                        Console.WriteLine("\r\nYour current balance is:" + reply3.Balance.ToString() + "\r\n");
                        break;
                    case ConsoleKey.X:
                        keepRunnning = false;
                        break;
                }
            }
        }

        public static void createChannels(string args)
        {
            string[] ports = args.Split('|');
            ports = ports.Take(ports.Count() - 1).ToArray();

            foreach (string port in ports)
            {
                var clientInterceptor = new ClientInterceptor();
                GrpcChannel channel = GrpcChannel.ForAddress("http://localhost:" + port);
                CallInvoker interceptingInvoker = channel.Intercept(clientInterceptor);
                BankClientService.BankClientServiceClient server = new BankClientService.BankClientServiceClient(interceptingInvoker);
                string host = "http://localhost:" + port;
                servers.Add(host, server);
            }
        }
}

    public class ClientInterceptor : Interceptor
    {
        // private readonly ILogger logger;

        //public GlobalServerLoggerInterceptor(ILogger logger) {
        //    this.logger = logger;
        //}

        public override TResponse BlockingUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
        {

            Metadata metadata = context.Options.Headers; // read original headers
            if (metadata == null) { metadata = new Metadata(); }
            metadata.Add("dad", "dad-value"); // add the additional metadata

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