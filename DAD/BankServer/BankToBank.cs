using Grpc.Core;

namespace BankServer
{
    public class BankBankService : BankToBankService.BankToBankServiceBase
    {
        public BankBankService()
        {
        }

        public override Task<GreetReply> Greeting(
            GreetRequest request, ServerCallContext context)
        {
            Console.WriteLine("Received an Hi from another Bank Server.");
            return Task.FromResult(Reg(request));
        }

        public GreetReply Reg(GreetRequest request)
        {
            return new GreetReply
            {
                Hi = true
            };
        }
    }
}