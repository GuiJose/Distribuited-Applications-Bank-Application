using Grpc.Core;

namespace BankServer
{
    // ChatServerService is the namespace defined in the protobuf
    // ChatServerServiceBase is the generated base implementation of the service
    public class BankService : BankClientService.BankClientServiceBase
    {
        private BankAccount account;
        public BankService(BankAccount account)
        {
            this.account = account;
        }
        public override Task<DepositReply> Deposit(
            DepositRequest request, ServerCallContext context)
        {
            while (BankServer.GetFrozen()){}
            return Task.FromResult(Reg(request, context));
        }

        public DepositReply Reg(DepositRequest request, ServerCallContext context)
        {
            Console.WriteLine("CHEGUEI A FUNCAO");
            Console.WriteLine("Received request to deposit");
            if (BankServer.getPrimary())
            {
                Console.WriteLine("SOU PRIMARIO");
                account.Deposit(request.Ammount);
                BankServer.Replica(context.RequestHeaders.GetValue("dad") + " D " +request.Ammount);
                return new DepositReply { Balance = account.GetBalance() };
            }
            //else BankServer.getCommands().Add(context.RequestHeaders.GetValue("dad"), "D " + request.Ammount.ToString());
            return new DepositReply { Balance = -1 };
        }

        public override Task<WithdrawalReply> Withdrawal(
            WithdrawalRequest request, ServerCallContext context)
        {
            while (BankServer.GetFrozen()) { }
            return Task.FromResult(Reg2(request, context));
        }

        public WithdrawalReply Reg2(WithdrawalRequest request, ServerCallContext context)
        {
            Console.WriteLine("Received request to Withdrawal");
            if (BankServer.getPrimary())
            {
                bool ok = account.Withdrawal(request.Ammount);
                return new WithdrawalReply { Balance = account.GetBalance(), Success = ok };
            }
            else BankServer.getCommands().Add(context.RequestHeaders.GetValue("dad"), "W " + request.Ammount.ToString());
            return new WithdrawalReply { Balance = -1, Success = false };
        }

        public override Task<ReadBalanceReply> ReadBalance(
            ReadBalanceRequest request, ServerCallContext context)
        {
            while (BankServer.GetFrozen()) { }
            return Task.FromResult(Reg3(request));
        }

        public ReadBalanceReply Reg3(ReadBalanceRequest request)
        {
            Console.WriteLine("Received request to read Balance");
            return new ReadBalanceReply {Balance = account.GetBalance() };
        }


    }
}
