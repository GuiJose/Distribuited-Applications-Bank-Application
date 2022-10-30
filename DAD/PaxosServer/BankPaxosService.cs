﻿using System.Runtime.ExceptionServices;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace PaxosServer
{   
    public class BankService : BankPaxosService.BankPaxosServiceBase
    { 
        private bool first = false;
        private bool decided = false;
        private int value = 0;
        private int idMessage = 0;
        private static object idMessageLock = new object();
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
            int id = 0;
            lock (idMessageLock)
            {
                idMessage++;
                id = idMessage;
            }
            if (!first)
            {
                first = true;
                value = await PaxosServer.Paxos(request.Value, request.Slot);
                decided = true;
            }

            while (!decided) { }

            if (id == 3)
            {
                decided = false;
                first = false;
                idMessage = 0;
            }
            return new CompareAndSwapReply { Value = value };
        }
    }
}