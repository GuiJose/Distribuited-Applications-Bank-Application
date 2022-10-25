using System.Runtime.ExceptionServices;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace PaxosServer
{
   
    public class BankService : BankPaxosService.BankPaxosServiceBase
    {
        private bool first = true;
        public BankService()
        {
        }

        public override Task<CompareAndSwapReply> CompareAndSwap(
            CompareAndSwapRequest request, ServerCallContext context)
        {
            return Reg(request);
        }

        public async Task<CompareAndSwapReply> Reg(CompareAndSwapRequest request)
        {
            int value = await PaxosServer.Paxos(request.Value, request.Slot);
            return new CompareAndSwapReply { Value = value };
        }
    }
}