using Grpc.Core;

namespace PaxosServer
{
    public class PaxosPaxosService : PaxosToPaxosService.PaxosToPaxosServiceBase
    {
        public PaxosPaxosService()
        {
        }

        public override Task<GreetReply2> Greeting2(
            GreetRequest2 request, ServerCallContext context)
        {
            Console.WriteLine("Received an Hi from another Paxos Server.");
            return Task.FromResult(Reg(request));
        }

        public GreetReply2 Reg(GreetRequest2 request)
        {
            return new GreetReply2
            {
                Hi = true
            };
        }
    }
}