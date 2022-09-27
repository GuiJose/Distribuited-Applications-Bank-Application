using Grpc.Core;
using Grpc.Core.Interceptors;

namespace BankClient
{
    // ChatServerService is the namespace defined in the protobuf
    // ChatServerServiceBase is the generated base implementation of the service
    public class BankService : BankClientService.BankClientServiceBase
    {
        private Dictionary<string, string> clientMap = new Dictionary<string, string>();

        public BankService()
        {
        }

        public override Task<RegisterReply> Register(
            RegisterRequest request, ServerCallContext context)
        {
            return Task.FromResult(Reg(request));
        }

        public RegisterReply Reg(RegisterRequest request)
        {

            lock (this)
            {
                Console.WriteLine("Received request to register");
            }
            return new RegisterReply{};
        }
    }
    class BankServer
    {
        private double balance;
        private readonly object balanceLock = new object();
        static void Main(string[] args)
        {
            const int ServerPort = 1001;
            const string ServerHostname = "localhost";

            Server server = new Server
            {
                Services = { BankClientService.BindService(new BankService()).Intercept(new ServerInterceptor()) },
                Ports = { new ServerPort(ServerHostname, ServerPort, ServerCredentials.Insecure) }
            };
            server.Start();
            Console.WriteLine("ChatServer server listening on port " + ServerPort);
            Console.WriteLine("Press any key to stop the server...");
            Console.ReadKey();

            server.ShutdownAsync().Wait();

        }

        private void deposit(double value)
        {
            lock (balanceLock)
            {
                balance += value;
            }
        }
        private bool withdrawal(double value)
        {
            bool success = false;
            lock (balanceLock)
            {
                if (balance < value)
                {
                    success = false;
                }
                else
                {
                    balance = balance - value;
                    success = true;
                }
            }
            return success;
        }
    }
}
    public class ServerInterceptor : Interceptor
    {

        public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
        {
            string callId = context.RequestHeaders.GetValue("dad");
            Console.WriteLine("DAD header: " + callId);
            return base.UnaryServerHandler(request, context, continuation);
        }

    }

