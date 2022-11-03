using Grpc.Core;

namespace PaxosServer
{
    public class PaxosPaxosService : PaxosToPaxosService.PaxosToPaxosServiceBase
    {
        private Paxos paxos;
        public PaxosPaxosService(Paxos paxos)
        {
            this.paxos = paxos;
        }

        public override Task<GreetReply> Greeting(
            GreetRequest request, ServerCallContext context)
        {
            Console.WriteLine("Received an Hi from another Paxos Server.");
            return Task.FromResult(Reg(request));
        }

        public GreetReply Reg(GreetRequest request)
        {
            return new GreetReply { Id = PaxosServer.getId() };
        }

        public override Task<Promise> Prepare(PrepareRequest request, ServerCallContext context)
        {
            while (PaxosServer.GetFrozen()) { continue; }
            return Task.FromResult(Promise(request));
        }


        public Promise Promise(PrepareRequest request)
        {
            Console.WriteLine("Recebi um prepare de:" + request.ProposerID);
            return new Promise { Value = { paxos.promise(request.ProposerID, request.Slot) } };
        }

        public override Task<Accepted_message> AcceptRequest(Accept request, ServerCallContext context)
        {
            return Task.FromResult(Acceptor(request));
        }


        public Accepted_message Acceptor(Accept request)
        {
            List<int> temp_list = paxos.accepted(request.Value, request.ProposerID);
            return new Accepted_message { ValuePromised = temp_list[0], ProposerID = temp_list[1] };
        }


        public override Task<CommitReply> Commit(CommitRequest request, ServerCallContext context)
        {
            return Task.FromResult(Commited(request));
        }


        public CommitReply Commited(CommitRequest request)
        {
            return new CommitReply { Ok = paxos.commit(request.Value, request.Slot) };
        }
        
    }
}