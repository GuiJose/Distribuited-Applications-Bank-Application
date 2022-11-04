namespace BankServer
{
    public class BankAccount
    {
        private static double balance = 0;
        private static object balanceLock = new object();

        public BankAccount()
        {
        }
        public void Deposit(double value)
        {
            lock (balanceLock)
            {
                balance += value;
            }
        }
        public bool Withdrawal(double value)
        {
            bool success = false;
            lock (balanceLock)
            {
                if (balance < value)
                {
                    success = false;
                }
                else
                {
                    balance = balance - value;
                    success = true;
                }
            }
            return success;
        }

        public double GetBalance()
        {
            lock (balanceLock)
            {
                return balance;
            }
        }
    }
}
