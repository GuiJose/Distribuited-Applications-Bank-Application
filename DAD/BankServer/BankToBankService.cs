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
            return new GreetReply { Id = BankServer.getId() };
        }


        public override Task<ReplicaReply> Replica(ReplicaRequest request, ServerCallContext context)
        {
            return Task.FromResult(Replicate(request));
        }

        public ReplicaReply Replicate(ReplicaRequest request)
        {
            BankServer.ReplicateCommands(request.Commands.ToList());
            return new ReplicaReply { };
        }
    }
}