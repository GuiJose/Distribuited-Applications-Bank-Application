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
        static void Main(string[] args)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            GrpcChannel channel = GrpcChannel.ForAddress("http://localhost:1001");
            var client = new BankPaxosService.BankPaxosServiceClient(channel);
            GreetRequest request = new GreetRequest{ Hi = true };
            Console.WriteLine("Press any key to contact the server");
            Console.ReadKey();
            client.Greeting(request);
            Console.WriteLine("The client will stop.");
        }
    }
}