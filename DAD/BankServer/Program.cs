using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace BankPaxosClient
{
    class Program
    {
        private static List<GrpcChannel> paxosChannels = new List<GrpcChannel>();
        private readonly object balanceLock = new object();
        double balance = 0;
        static async Task Main(string[] args)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            GrpcChannel channel = GrpcChannel.ForAddress("http://localhost:1001");
            var client = new BankPaxosService.BankPaxosServiceClient(channel);
            GrpcChannel channel2 = GrpcChannel.ForAddress("http://localhost:1002");
            var client2 = new BankPaxosService.BankPaxosServiceClient(channel2);
            GrpcChannel channel3 = GrpcChannel.ForAddress("http://localhost:1003");
            var client3 = new BankPaxosService.BankPaxosServiceClient(channel3);

            paxosChannels.Add(channel);
            paxosChannels.Add(channel2);
            paxosChannels.Add(channel3);

            GreetRequest request = new GreetRequest { Hi = true };
            Console.WriteLine("Press any key to contact the server");
            Console.ReadKey();
            var reply = await client.GreetingAsync(request);
            Console.WriteLine($"{reply.Hi}");
            Console.WriteLine("The client will stop.");
            Console.ReadKey();
        }

        private void deposit(double value)
        {
            lock (balanceLock)
            {
                balance += value;
            }
        }
        private bool withdrawal(double value)
        {
            bool success = false;
            lock (balanceLock)
            {
                if (balance < value)
                {
                    success = false;
                }
                else
                {
                    balance = balance - value;
                    success = true;
                }
            }
            return success;
        }
    }
}