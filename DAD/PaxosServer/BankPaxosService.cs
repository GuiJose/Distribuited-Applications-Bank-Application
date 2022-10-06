using Grpc.Core;

namespace PaxosServer
{
    public class BankService : BankPaxosService.BankPaxosServiceBase
    {
        public BankService()
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
}