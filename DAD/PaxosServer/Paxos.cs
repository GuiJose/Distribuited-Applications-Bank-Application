namespace PaxosServer
{
    public class Paxos
    {
        private int ID;
        private int value = 45;
        private int write_ts = 0;
        private int read_ts = 0;
        int pedido = 1;
        public Paxos(int iD)
        {
            this.ID = iD;
        }

        public List<int> promise(int id)
        {
            
            Console.WriteLine("recebi o pedido do ID" + id + " pedido" + pedido + "READ = " + read_ts);

            if (id > read_ts)
            {
                lock (this) { read_ts = id; }

                pedido++;
                return new List<int> { write_ts, value };
            }
            pedido++;
            return new List<int>();
        }

        public int getID()
        {
            return ID;
        }
    }
}
