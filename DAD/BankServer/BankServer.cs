using Grpc.Core;
using Grpc.Core.Interceptors;

namespace BankServer
{
    class BankServer
    {
        private static bool frozen = false;
        private static double balance = 0;
        private static readonly object balanceLock = new object();
        private static readonly object frozenLock = new object();
        static void Main(string[] args)
        {
            bool keepRunning = true;
            const int ServerPort = 1001;
            const string ServerHostname = "localhost";

            Server server = new Server
            {
                Services = { BankClientService.BindService(new BankService()).Intercept(new ServerInterceptor()) },
                Ports = { new ServerPort(ServerHostname, ServerPort, ServerCredentials.Insecure) }
            };
            server.Start();

            Console.WriteLine("ChatServer server listening on port " + ServerPort);

            while (keepRunning)
            {
                Console.WriteLine("Press 'F' to frozen the process, 'N' to put the process in its normal condition or press" +
                    "'X' to finish the process.\r\n");
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
                    case ConsoleKey.X:
                        keepRunning = false;
                        break;
                }
            }
            server.ShutdownAsync().Wait();
        }

        public static void Deposit(double value)
        {
            lock (balanceLock)
            {
                balance += value;
            }
        }
        public static bool Withdrawal(double value)
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

        public static double GetBalance() {
            lock (balanceLock)
            {
                return balance;
            }
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
    public class ServerInterceptor : Interceptor
    {

        public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
        {
            string callId = context.RequestHeaders.GetValue("dad");
            //Console.WriteLine("DAD header: " + callId);
            return base.UnaryServerHandler(request, context, continuation);
        }

    }
