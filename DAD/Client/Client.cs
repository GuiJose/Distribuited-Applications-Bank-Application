using BankClient;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using System.Globalization;

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
            string[] operationsText = File.ReadAllLines(args[1]);
            Console.WriteLine("ID = " + id);
            bool keepRunnning = true;
            createChannels(args);
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            sleep(2000);
            
            executeFileCommmands(operationsText);

            while (keepRunnning)
            {
                Console.WriteLine("Hi there, press 'D' to make a deposit, 'W' to make a withdrawal or 'R' to read your current balance.\r\n" +
                    "In case, you want to finish your activity press 'X'\r\n");
                switch (Console.ReadKey().Key)
                {
                    case ConsoleKey.D:
                        Console.WriteLine("\r\nHow much do you want to deposit ?\r\n");
                        double ammount = double.Parse(Console.ReadLine(), CultureInfo.InvariantCulture);
                        executeDeposit(ammount);
                        break;
                    
                    case ConsoleKey.W:
                        Console.WriteLine("\r\nHow much do you want to withdrawal ?\r\n");
                        double ammount2 = double.Parse(Console.ReadLine(), CultureInfo.InvariantCulture);
                        executeWithdrawal(ammount2);
                        break;

                    case ConsoleKey.R:
                        executeRead();
                        break;

                    case ConsoleKey.X:
                        keepRunnning = false;
                        break;
                }
            }
        }
        private static void executeFileCommmands(string[] configurationText)
        {
            foreach (string line in configurationText)
            {
                if (line[0] == 'D')
                {
                    executeDeposit(double.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture));
                }
                else if (line[0] == 'W')
                {
                    executeWithdrawal(double.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture));
                }
                else if (line[0] == 'R')
                {
                    executeRead();
                }
                else if (line[0] == 'S')
                {
                    sleep(int.Parse(line.Split(' ')[1]));
                }
                
            }
        }
        private static void sleep(int t)
        {
            System.Threading.Thread.Sleep(t);
        }

        private static void executeDeposit(double ammount)
        {
            Console.WriteLine("You are trying to deposit " + ammount.ToString() + " euros.");
            DepositRequest request = new DepositRequest { Ammount = ammount };
            foreach (KeyValuePair<string, BankClientService.BankClientServiceClient> server in servers)
            {
                DepositReply reply = server.Value.Deposit(request);
                if (reply.Balance != -1)
                    Console.WriteLine("Well Done! Your current balance is:" + reply.Balance.ToString() + "\r\n");
                else continue;
            }
        }
        private static void executeWithdrawal(double ammount)
        {
            Console.WriteLine("You are trying to withdrawal " + ammount.ToString() + " euros.");
            WithdrawalRequest request = new WithdrawalRequest { Ammount = ammount };
            foreach (KeyValuePair<string, BankClientService.BankClientServiceClient> server in servers)
            {
                WithdrawalReply reply = server.Value.Withdrawal(request);
                if (reply.Success)
                {
                    Console.WriteLine("Well Done! Your current balance is:" + reply.Balance.ToString() + "\r\n");
                }
                else if (!reply.Success && reply.Balance != -1)
                {
                    Console.WriteLine("You don't have enough money! Your current balance is:" + reply.Balance.ToString() + "\r\n");
                }
                else
                {
                    continue;
                }
            }
        }

        private static void executeRead()
        {
            Console.WriteLine("You requested a balance read.");
            ReadBalanceRequest request = new ReadBalanceRequest { };
            foreach (KeyValuePair<string, BankClientService.BankClientServiceClient> server in servers)
            {
                ReadBalanceReply reply = server.Value.ReadBalance(request);
                Console.WriteLine("\r\nYour current balance is:" + reply.Balance.ToString() + "\r\n");
            }
        }

        private static void createChannels(string[] args)
        {
            for(int i = 2; i < args.Length; i++)
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