namespace PaxosServer
{
    public class Paxos
    {
        private int ID;
        private int value = 0;
        private int write_ts = 0; //so e trocado no accept
        private int read_ts = 0;
        int pedido = 1;
        public Paxos(int iD)
        {
            this.ID = iD;
        }

        public List<int> promise(int id)
        {
            
            Console.WriteLine("recebi o pedido do ID " + id + " pedido " + pedido + " READ = " + read_ts);

            if (id > read_ts)
            {
                lock (this) { read_ts = id; }

                pedido++;
                return new List<int> { write_ts,value };
            }
            pedido++;
            return new List<int> {};
        }
        public List<int> accepted(int id, int value_to_accept)
        {
            if (read_ts == id) { write_ts = id; value = value_to_accept; };
            return new List<int> { id, value_to_accept };
        }
            public int getID()
        {
            return ID;
        }


    }
}
