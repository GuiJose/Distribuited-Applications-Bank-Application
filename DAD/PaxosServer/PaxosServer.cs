using Grpc.Core;
using Grpc.Core.Interceptors;

namespace PaxosServer
{
    class PaxosServer
    {

        static int Port;
        static void Main(string[] args)
        {
            Port = Int16.Parse(args[0]);
            Console.WriteLine(Port);
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            Server server = new Server
            {
                Services = { BankPaxosService.BindService(new ServerService()).Intercept(new PaxosServerInterceptor()) },
                Ports = { new ServerPort("localhost", Port, ServerCredentials.Insecure) }
            };
            server.Start();
            Console.WriteLine("PaxosServer server listening on port " + Port);
            Console.WriteLine("Press any key to stop the server...");
            Console.ReadKey();

            server.ShutdownAsync().Wait();
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
