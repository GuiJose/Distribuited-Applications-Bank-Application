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
            return Task.FromResult(Reg(request));
        }

        public DepositReply Reg(DepositRequest request)
        {
            Console.WriteLine("Received request to deposit");
            account.Deposit(request.Ammount);
            return new DepositReply { Balance = account.GetBalance() };
        }

        public override Task<WithdrawalReply> Withdrawal(
            WithdrawalRequest request, ServerCallContext context)
        {
            while (BankServer.GetFrozen()) { }
            return Task.FromResult(Reg2(request));
        }

        public WithdrawalReply Reg2(WithdrawalRequest request)
        {
            Console.WriteLine("Received request to read Balance");
            bool ok = account.Withdrawal(request.Ammount);
            return new WithdrawalReply { Balance = account.GetBalance(), Success = ok};
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
