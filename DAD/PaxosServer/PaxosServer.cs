using Grpc.Core;

namespace PaxosServer
{
    public class ServerService : BankPaxosService.BankPaxosServiceBase
    {
        public ServerService()
        {
        }

        public override Task<GreetReply> Greeting(
            GreetRequest request, ServerCallContext context)
        {
            return Task.FromResult(Reg(request));
        }

        public GreetReply Reg(GreetRequest request)
        {
            Console.WriteLine("Received an hi from bank server.");
            return new GreetReply
            {
                Hi = true
            };
        }
    }
    class PaxosServer
    {
        const int Port = 1001;
        static void Main(string[] args)
        {
            Server server = new Server
            {
                Services = { BankPaxosService.BindService(new ServerService()) },
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