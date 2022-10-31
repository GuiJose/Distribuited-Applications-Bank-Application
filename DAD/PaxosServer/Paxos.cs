namespace PaxosServer
{
    public class Paxos
    {
        private int ID;
        private int value = 0;
        private int write_ts = 0; //so e trocado no accept
        private int read_ts = 0;
        private Dictionary<int, int> decisions = new Dictionary<int, int>();
        int pedido = 1;
        public Paxos(int iD)
        {
            this.ID = iD;
        }

        public List<int> promise(int id, int slot)
        {
            if (!decisions.ContainsKey(slot))
            {
                if (id > read_ts)
                {
                    lock (this) { read_ts = id; }
                    return new List<int> { write_ts, value };
                }
            }
            return new List<int> { };
        }
        public List<int> accepted(int id, int value_to_accept)
        {
            if (read_ts == id)
            {
                lock (this)
                {
                    write_ts = id;
                    value = value_to_accept;
                }
            };

            return new List<int> { id, value_to_accept };
        }
        public int getID() { return ID; }

        public bool commit(int value, int slot)
        { 
            if (!decisions.ContainsKey(slot))
            {
                lock (this) 
                { 
                    decisions.Add(slot, value);
                    value = 0;
                    write_ts = 0;
                    read_ts = 0;
                }
                return true;
            }
            return false;
        }

        public bool hasSlot(int slot) { return decisions.ContainsKey(slot); }
        public int getSlot(int slot) { return decisions[slot]; }
    }
}
