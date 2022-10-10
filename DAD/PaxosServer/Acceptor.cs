using Grpc.Core;

namespace PaxosServer
{
    public class Acceptor
    {
        private int acceptedProposerNumber = -1;
        private int acceptedProposedValue = -1;
        private int promissedProposerNumber = -1;

        public bool Accept(int proposerNumber, int value)
        {
            if(proposerNumber < promissedProposerNumber) return false;
            acceptedProposerNumber = promissedProposerNumber;
            acceptedProposedValue = value;

            if (proposerNumber > promissedProposerNumber) promissedProposerNumber = proposerNumber; 
            return true;
        }
    }
}
