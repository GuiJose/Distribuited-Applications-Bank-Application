using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaxosServer
{
    public class Paxos
    {
        private Proposer[] proposers;
        private Acceptor[] acceptors;
        private Learner[] learners;
        private int agentNumbers;

        public Paxos(int value)
        {
            agentNumbers = value;
            proposers = new Proposer[value];
            acceptors = new Acceptor[value];
            learners = new Learner[value];

            for(int i = 0; i< value; i++)
            {
                learners[i] = new Learner();
                acceptors[i] = new Acceptor();
                proposers[i] = new Proposer(i, agentNumbers, acceptors);
            }
        }

        public Proposer[] getProposers() { return proposers; }
        public Acceptor[] getAcceptors() { return acceptors; }
        public Learner[] getLearners() { return learners; }

    }


}
