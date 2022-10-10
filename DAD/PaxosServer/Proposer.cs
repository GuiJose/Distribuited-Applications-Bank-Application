using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;

namespace PaxosServer
{
    public class Proposer
    {
        private int proposerNumber;
        private int numberOfProposers;
        Acceptor[] acceptors;

        public Proposer(int proposerNumber, int numberOfProposers, Acceptor[] acceptors)
        {
            this.proposerNumber = proposerNumber;
            this.numberOfProposers = numberOfProposers;
            this.acceptors = acceptors;
        }

        public bool Accept(int value, int proposerNumber)
        {
            AcceptRequest request = new AcceptRequest { ProposerNumber = proposerNumber , Value = value};
            AcceptReply reply = acceptors.
            return 

        }
    }
}
