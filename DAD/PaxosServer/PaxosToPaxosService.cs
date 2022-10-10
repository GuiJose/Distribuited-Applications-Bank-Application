using Grpc.Core;

namespace PaxosServer
{
    public class PaxosPaxosService : PaxosToPaxosService.PaxosToPaxosServiceBase
    {
        private Acceptor[] acceptors;
        private Learner[] learners;
        private Proposer[] proposers;

        public PaxosPaxosService(Acceptor[] acceptors, Learner[] learners, Proposer[] proposers)
        {
            this.acceptors = acceptors;
            this.learners = learners;
            this.proposers = proposers;
        }

        public override Task<GreetReply2> Greeting2(
            GreetRequest2 request, ServerCallContext context)
        {
            Console.WriteLine("Received an Hi from another Paxos Server.");
            return Task.FromResult(Reg(request));
        }

        public GreetReply2 Reg(GreetRequest2 request)
        {
            return new GreetReply2
            {
                Hi = true
            };
        }

        public override Task<AcceptReply> Accept( AcceptRequest request, ServerCallContext context)
        {
            return Task.FromResult(Accepted(request));
        }

        public AcceptReply Accepted( AcceptRequest request)
        {
            int acceptedCount = 0;
            for(int i = 0; i < acceptors.Length; i++)
            {
                if (acceptors[i].Accept(request.ProposerNumber, request.Value)) acceptedCount++;
            }

            if (acceptedCount > acceptors.Length / 2) return new AcceptReply { Accepted = true };
            
            return new AcceptReply { Accepted = false };
        }

    }
}