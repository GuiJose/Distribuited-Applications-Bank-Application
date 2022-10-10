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

        //public override Task<Accepted_message> AcceptRequest(Accept request, ServerCallContext context)
        //{
          //  return Task.FromResult(Accepted(request));
        //}


        //public Accepted_message Accepted(Accept request)
        //{
            //int acceptedCount = 0;
            //for (int i = 0; i < acceptors.Length; i++)
            //{
              //  if (acceptors[i].Accept(request.ProposerID, request.Value)) acceptedCount++;
            //}

            //if (acceptedCount > acceptors.Length / 2) return new Accepted_message { Accepted = true };

            //return new Accepted_message { Accepted = false };
        //}

    }
}