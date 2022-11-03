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
            while (BankServer.GetFrozen())
            {
                Monitor.Wait(BankServer.getFrozenObject());
            }
            return Task.FromResult(Reg(request, context));
        }

        public DepositReply Reg(DepositRequest request, ServerCallContext context)
        {
            Console.WriteLine("Received request to Deposit");
            string key = context.RequestHeaders.GetValue("dad");
            BankServer.AddCommands(key, "D " + request.Ammount.ToString());

            if (BankServer.getPrimary())
            {
                BankServer.executeCommands(key);
                return new DepositReply { Balance = account.GetBalance() };
            }
            else
            {
                return new DepositReply { Balance = -1.0 };
            }
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
            string key = context.RequestHeaders.GetValue("dad");
            BankServer.AddCommands(key, "W " + request.Ammount.ToString());
            if (BankServer.getPrimary())
            {
                bool ok = BankServer.executeCommands(key);
                return new WithdrawalReply { Balance = account.GetBalance(), Success = true };
            }
            else
            {
                return new WithdrawalReply { Balance = -1, Success = false };
            }
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
