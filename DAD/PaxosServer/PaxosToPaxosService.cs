using Google.Protobuf.WellKnownTypes;
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

        public override Task<Promise> PrepareRequest(Prepare request, ServerCallContext context)
        {
            return Task.FromResult(Promise(request));
        }


        public Promise Promise(Prepare request)
        {
            return new Promise { Value = { paxos.promise(request.ProposerID) } };
        }



        public override Task<Accepted_message> AcceptRequest(Accept request, ServerCallContext context)
        {
            return Task.FromResult(Acceptor(request));
        }


        public Accepted_message Acceptor(Accept request)
        {
            return new Accepted_message { Value = { paxos.promise(request.ProposerID) } };
        }
        

    }
}