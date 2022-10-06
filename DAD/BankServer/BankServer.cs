using BankPaxosClient;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;

namespace BankServer
{
    class BankServer
    {
        static int port;
        private static BankAccount account = new BankAccount();
        private static bool frozen = false;
        private static readonly object frozenLock = new object();
        static void Main(string[] args)
        {
            bool keepRunning = true;
            port = Int16.Parse(args[0]);
            const int PaxosPort = 1002;
            const string ServerHostname = "localhost";

            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            Server server = new Server
            {
                Services = { BankClientService.BindService(new BankService(account)).Intercept(new BankServerInterceptor()) },
                Ports = { new ServerPort(ServerHostname, port, ServerCredentials.Insecure) }
            };
            server.Start();

            GrpcChannel channel = GrpcChannel.ForAddress("http://" + ServerHostname + ":" + PaxosPort);
            CallInvoker interceptingInvoker = channel.Intercept(new BankServerInterceptor());
            var paxosServer = new BankPaxosService.BankPaxosServiceClient(interceptingInvoker);

            Console.WriteLine("ChatServer server listening on port " + port);

            while (keepRunning)
            {
                Console.WriteLine("Press 'F' to frozen the process, 'N' to put the process in its normal condition or press" +
                    "'X' to finish the process.\r\n In case you want to greet the PaxosServer press 'A'\r\n");
                switch (Console.ReadKey().Key)
                {
                    case ConsoleKey.F:
                        lock (frozenLock)
                        {
                            frozen = true;
                        }
                        break;
                    case ConsoleKey.N:
                        lock (frozenLock)
                        {
                            frozen = false;
                        }
                        break;
                    case ConsoleKey.A:
                        GreetRequest request = new GreetRequest { Hi = true };
                        GreetReply reply = paxosServer.Greeting(request);
                        Console.WriteLine(reply.ToString());
                        break;

                    case ConsoleKey.X:
                        keepRunning = false;
                        break;

                }
            }
            server.ShutdownAsync().Wait();
        }
        public static bool GetFrozen()
        {
            lock (frozenLock)
            {
                return frozen;
            }
        }
    }
}
    public class BankServerInterceptor : Interceptor
    {
        public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
        {
            string callId = context.RequestHeaders.GetValue("dad");
            //Console.WriteLine("DAD header: " + callId);
            return base.UnaryServerHandler(request, context, continuation);
        }
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
