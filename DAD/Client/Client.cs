using BankClient;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;

namespace Client
{
    static class BankClient
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            bool keepRunnning = true;
            const int ServerPort = 1001;
            const string ServerHostname = "localhost";
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            var clientInterceptor = new ClientInterceptor();
            GrpcChannel channel = GrpcChannel.ForAddress("http://" + ServerHostname + ":" + ServerPort);
            CallInvoker interceptingInvoker = channel.Intercept(clientInterceptor);
            var server = new BankClientService.BankClientServiceClient(interceptingInvoker);

            while (keepRunnning)
            {
                Console.WriteLine("Hi there, press 'D' to make a deposit, 'W' to make a withdrawal or 'R' to read your current balance.\r\n" +
                    "In case, you want to finish your activity press 'X'\r\n");
                switch (Console.ReadKey().Key)
                {
                    case ConsoleKey.D:
                        Console.WriteLine("How much do you want to deposit ?\r\n");
                        double ammount = Convert.ToDouble(Console.ReadLine());
                        DepositRequest request = new DepositRequest{Ammount = ammount};
                        DepositReply reply = server.Deposit(request);
                        Console.WriteLine("Well Done! Your current balance is:" + reply.Balance.ToString() + "\r\n");
                        break;
                    
                    case ConsoleKey.W:
                        Console.WriteLine("How much do you want to withdrawal ?\r\n");
                        double ammount2 = Convert.ToDouble(Console.ReadLine());
                        WithdrawalRequest request2 = new WithdrawalRequest { Ammount = ammount2};
                        WithdrawalReply reply2 = server.Withdrawal(request2);
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
                        ReadBalanceReply reply3 = server.ReadBalance(request3);
                        Console.WriteLine("Your current balance is:" + reply3.Balance.ToString() + "\r\n");
                        break;
                    case ConsoleKey.X:
                        keepRunnning = false;
                        break;
                }
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