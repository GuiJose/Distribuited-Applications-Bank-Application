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
            return Task.FromResult(Promise(request));
        }


        public Promise Promise(PrepareRequest request)
        {
            Console.WriteLine("Recebi um prepare de:" + request.ProposerID);
            return new Promise { Value = { paxos.promise(request.ProposerID) } };
        }

<<<<<<< HEAD

/*
        public override Task<Accepted_message> AcceptRequest(Accept request, ServerCallContext context)
=======
        public override Task<AliveResponse> Alive(AliveRequest request, ServerCallContext context)
>>>>>>> c7f81360ef4b66ec9006b03925ab463f4cdbbab5
        {
            return Task.FromResult(Alive2(request));
        }


        public AliveResponse Alive2(AliveRequest request)
        {
<<<<<<< HEAD
            return new Accepted_message { ProposerID = { paxos.promise(request.ProposerID) } };
        }
        
        */
=======
            PaxosServer.resetTimer(request.Id);
            PaxosServer.setFrozenFalse(request.Id);
            Console.WriteLine("Recebi um alive do:" + request.Id);
            return new AliveResponse {} ;
        }

        //public override Task<Accepted_message> AcceptRequest(Accept request, ServerCallContext context)
        //{
        //  return Task.FromResult(Acceptor(request));
        //}


        //public Accepted_message Acceptor(Accept request)
        //{
        //return new Accepted_message { Value_promised = { paxos.promise(request.ProposerID) } };
        //}
>>>>>>> c7f81360ef4b66ec9006b03925ab463f4cdbbab5
    }
}