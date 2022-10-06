using Grpc.Core;

namespace PaxosServer
{
    public class BankService : BankPaxosService.BankPaxosServiceBase
    {
        public BankService()
        {
        }

        public override Task<GreetReply3> Greeting(
            GreetRequest3 request, ServerCallContext context)
        {
            return Task.FromResult(Reg(request));
        }

        public GreetReply3 Reg(GreetRequest3 request)
        {
            Console.WriteLine("Received an hi from bank server.");
            return new GreetReply3
            {
                Hi = true
            };
        }
    }
}