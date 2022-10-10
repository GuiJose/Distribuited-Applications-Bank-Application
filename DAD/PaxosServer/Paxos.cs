using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaxosServer
{
    public class Paxos
    {
        private int ID;
        private int value;
        private int write_ts;
        private int read_ts;
        public Paxos(int iD)
        {
            ID = iD;
        }

        public List<int> promise(int id)
        {
            Console.WriteLine("recebi o pedido");
            if (id > read_ts)
            {
                read_ts = id;
                return new List<int> {write_ts,value};
            }
            return new List<int>();
        }
    }
}
