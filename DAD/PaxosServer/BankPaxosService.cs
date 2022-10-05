using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaxosServer
{
    public class ServerService : BankPaxosService.BankPaxosServiceBase
    {
        public ServerService()
        {
        }

        public override Task<GreetReply> Greeting(
            GreetRequest request, ServerCallContext context)
        {
            return Task.FromResult(Reg(request));
        }

        public GreetReply Reg(GreetRequest request)
        {
            Console.WriteLine("Received an hi from bank server.");
            return new GreetReply
            {
                Hi = true
            };
        }
    }
}