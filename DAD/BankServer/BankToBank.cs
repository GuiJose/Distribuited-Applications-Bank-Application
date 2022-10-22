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
            if (!BankServer.getBankID().Contains(request.Id))
            {
                BankServer.getBankID().Add(request.Id);
                return new GreetReply { Hi = true };
            }
            return new GreetReply { Hi = false };
        }


        public override Task<ReplicaReply> Replica(ReplicaRequest request, ServerCallContext context)
        {
            return Task.FromResult(Replicate(request));
        }

        public ReplicaReply Replicate(ReplicaRequest request)
        {
            BankServer.executeCommands(request.Key);
            return new ReplicaReply { };
        }
    }
}