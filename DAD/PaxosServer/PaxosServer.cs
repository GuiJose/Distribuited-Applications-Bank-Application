using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;

namespace PaxosServer
{
    public class PaxosServer
    {
        private static int Port;
        private static Paxos paxos;
        private static Dictionary<string, PaxosToPaxosService.PaxosToPaxosServiceClient> otherPaxosServers = new Dictionary<string, PaxosToPaxosService.PaxosToPaxosServiceClient>();
        static void Main(string[] args)
        {
            paxos = new Paxos(Int16.Parse(args[0]));
            Port = Int16.Parse(args[1]);
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            Server server = new Server
            {
                Services = { BankPaxosService.BindService(new BankService()).Intercept(new PaxosServerInterceptor()),  
                PaxosToPaxosService.BindService(new PaxosPaxosService(paxos)).Intercept(new PaxosServerInterceptor())},
                Ports = { new ServerPort("localhost", Port, ServerCredentials.Insecure) }
            };
            server.Start();
            Console.WriteLine("PaxosServer server listening on port " + Port);

            createChannels(args);

            if (Port != 10000)
            {
                otherPaxosServers["http://localhost:10000"].PrepareRequestAsync(new Prepare { ProposerID = 2 });
            }

            Console.WriteLine("Press any key to stop the server...");
            Console.ReadKey();
            server.ShutdownAsync().Wait();
        }
        private static void createChannels(string[] args)
        {
            for (int i = 2; i < args.Length; i++)
            {
                GrpcChannel channel = GrpcChannel.ForAddress("http://localhost:" + args[i]);
                CallInvoker interceptingInvoker = channel.Intercept(new PaxosServerInterceptor());
                PaxosToPaxosService.PaxosToPaxosServiceClient server = new PaxosToPaxosService.PaxosToPaxosServiceClient(interceptingInvoker);
                string host = "http://localhost:" + args[i];
                otherPaxosServers.Add(host, server);
            }
        }

    }
}

public class PaxosServerInterceptor : Interceptor
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
