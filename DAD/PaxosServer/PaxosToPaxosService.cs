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

        public override Task<Promise> Prepare(PrepareRequest request, ServerCallContext context)
        {
            while (PaxosServer.GetFrozen()) { continue; }
            return Task.FromResult(Promise(request));
        }


        public Promise Promise(PrepareRequest request)
        {
            Console.WriteLine("Recebi um prepare de:" + request.ProposerID);
            return new Promise { Value = { paxos.promise(request.ProposerID) } };
        }

        public override Task<AliveResponse> Alive(AliveRequest request, ServerCallContext context)
        {
            while (PaxosServer.GetFrozen()) { continue; }
            return Task.FromResult(Alive2(request));
        }


        public AliveResponse Alive2(AliveRequest request)
        {
            PaxosServer.resetTimer(request.Id);
            PaxosServer.setFrozenFalse(request.Id);
            Console.WriteLine("Recebi um alive do:" + request.Id);
            return new AliveResponse {} ;
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
    }
}