using Grpc.Core;

namespace BankServer
{
    // ChatServerService is the namespace defined in the protobuf
    // ChatServerServiceBase is the generated base implementation of the service
    public class BankService : BankClientService.BankClientServiceBase
    {
        public BankService()
        {
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
            BankServer.Deposit(request.Ammount);
            return new DepositReply { Balance = BankServer.GetBalance() };
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
            bool ok = BankServer.Withdrawal(request.Ammount);
            return new WithdrawalReply { Balance = BankServer.GetBalance(), Success = ok};
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
            return new ReadBalanceReply {Balance = BankServer.GetBalance() };
        }
    }
}
