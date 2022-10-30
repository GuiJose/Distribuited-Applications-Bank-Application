using System.Collections.Generic;
using System.Data;
using System.Runtime.CompilerServices;
using System.Timers;
using BankPaxosClient;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;

namespace BankServer
{
    public class BankServer
    {
        private static int id;
        private static int slot = 1;
        private static int port;
        private static bool primary = false;
        private static List<int> banksID = new List<int>();
        private static BankAccount account = new BankAccount();
        private static bool frozen = false;
        private static readonly object frozenLock = new object();
        private static Dictionary<string, BankToBankService.BankToBankServiceClient> otherBankServers = new Dictionary<string, BankToBankService.BankToBankServiceClient>();
        private static Dictionary<string, BankPaxosService.BankPaxosServiceClient> PaxosServers = new Dictionary<string, BankPaxosService.BankPaxosServiceClient>();
        private static Dictionary<string, string> commands = new Dictionary<string, string>();
        private static List<String> replicateCommands = new List<string>();
        static void Main(string[] args)
        {
            bool keepRunning = true;
            id = Int16.Parse(args[0]);
            int numberBanks = Int16.Parse(args[1]);
            port = Int16.Parse(args[2]);
            banksID.Add(id);
            const string ServerHostname = "localhost";
            Console.WriteLine(id);
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            Server server = new Server
            {
                Services = { BankClientService.BindService(new BankService(account)).Intercept(new BankServerInterceptor()),
                BankToBankService.BindService(new BankBankService()).Intercept(new BankServerInterceptor())},
                Ports = { new ServerPort(ServerHostname, port, ServerCredentials.Insecure) }
            };
            server.Start();
            Console.WriteLine("ChatServer server listening on port " + port);


            createChannels(args, numberBanks);
            GreetBankServers();


            var paxos = new System.Timers.Timer(TimeSpan.FromSeconds(10).TotalMilliseconds);

            paxos.AutoReset = true;
            paxos.Elapsed += PrimaryElection;
            paxos.Start();

            var replica = new System.Timers.Timer(TimeSpan.FromSeconds(5).TotalMilliseconds);

            replica.AutoReset = true;
            replica.Elapsed += Replica;
            replica.Start();

            while (keepRunning)
            {
                Console.WriteLine("Press 'F' to frozen the process, 'N' to put the process in its normal condition or press" +
                    "'X' to finish the process.\r\n In case you want to greet the PaxosServer press 'A'\r\n");
                switch (Console.ReadKey().Key)
                {
                    case ConsoleKey.F:
                        lock (frozenLock)
                        {
                            frozen = true;
                        }
                        break;
                    case ConsoleKey.N:
                        lock (frozenLock)
                        {
                            frozen = false;
                        }
                        break;
                    case ConsoleKey.A:
                        //GreetRequest request = new GreetRequest { Hi = true };
                        //GreetReply reply = paxosServer.Greeting(request);
                        //Console.WriteLine(reply.ToString());
                        break;

                    case ConsoleKey.X:
                        keepRunning = false;
                        break;

                }
            }
            server.ShutdownAsync().Wait();
        }

        public static bool GetFrozen()
        {
            lock (frozenLock)
            {
                return frozen;
            }
        }
        private static void createChannels(string[] args, int numberBanks)
        {
            for (int i = 3; i < args.Length; i++)
            {
                if (i < (3 + numberBanks-1))
                {
                    GrpcChannel channel = GrpcChannel.ForAddress("http://localhost:" + args[i]);
                    CallInvoker interceptingInvoker = channel.Intercept(new BankServerInterceptor());
                    BankToBankService.BankToBankServiceClient server = new BankToBankService.BankToBankServiceClient(interceptingInvoker);
                    string host = "http://localhost:" + args[i];
                    otherBankServers.Add(host, server);
                }
                else
                {
                    GrpcChannel channel = GrpcChannel.ForAddress("http://localhost:" + args[i]);
                    CallInvoker interceptingInvoker = channel.Intercept(new BankServerInterceptor());
                    BankPaxosService.BankPaxosServiceClient server = new BankPaxosService.BankPaxosServiceClient(interceptingInvoker);
                    string host = "http://localhost:" + args[i];
                    PaxosServers.Add(host, server);
                }
            }
        }

        private static void PrimaryElection(object sender, ElapsedEventArgs e)
        {
            Console.WriteLine("ENVIEI PEDIDO DE PAXOS");
            foreach (KeyValuePair<string, BankPaxosService.BankPaxosServiceClient> paxosserver in PaxosServers)
            {
                CompareAndSwapReply reply = paxosserver.Value.CompareAndSwap(new CompareAndSwapRequest { Value = id, Slot = slot });
                Console.WriteLine("Slot: " + slot.ToString());
                if (reply.Value == 0)
                {
                    Console.WriteLine("Deu continue");
                    continue;
                }
                    
                if (reply.Value == id) setPrimary(true);
                else 
                {
                    if (primary)
                    {
                        Console.WriteLine("VOU REPLICAR PORQUE DEIXEI DE SER LÍDER");
                        foreach (KeyValuePair<string, BankToBankService.BankToBankServiceClient> server in otherBankServers)
                        {
                            ReplicaRequest request = new ReplicaRequest();
                            foreach (string rcmd in replicateCommands) request.Commands.Add(rcmd);
                            server.Value.ReplicaAsync(request);
                        }
                        replicateCommands.Clear();
                    }
                    setPrimary(false);
                }
                Console.WriteLine(reply.Value.ToString() + "\r\n");
                Console.WriteLine(primary ? "SOU PRIMARIO" : "NAO SOU PRIMARIO");
            }
            slot++;
        }
        
        private static void Replica(object sender, ElapsedEventArgs e)
        {
            if (primary)
            {
                Console.WriteLine("VOU REPLICAR");
                foreach (KeyValuePair<string, BankToBankService.BankToBankServiceClient> server in otherBankServers)
                {
                    ReplicaRequest request = new ReplicaRequest();
                    foreach (string rcmd in replicateCommands) request.Commands.Add(rcmd);
                    ReplicaReply reply = server.Value.Replica(request);
                }
                replicateCommands.Clear();
            }
        }

        private static void GreetBankServers()
        {
            int count = 0;
            foreach (KeyValuePair<string, BankToBankService.BankToBankServiceClient> server in otherBankServers)
            {
                GreetReply reply = server.Value.Greeting(new GreetRequest { Id = id });
                if (reply.Hi) count++;
            }
            if (count == otherBankServers.Count && id == banksID.Min()) setPrimary(true);          
        }

        public static List<int> getBankID() { return banksID; }

        private static void setPrimary(bool value) { primary = value; }

        public static bool getPrimary() { return primary; }

        public static Dictionary<string,string> getCommands() { return commands; }

        public static void AddCommands( string key, string value) { 
            commands.Add(key, value);
        }

        public static Dictionary<string ,BankToBankService.BankToBankServiceClient> getOtherBankServers() { return otherBankServers; }

        public static void Replica (ReplicaRequest request)
        {
            foreach (KeyValuePair<string, BankToBankService.BankToBankServiceClient> server in otherBankServers)
            {
                server.Value.Replica(request);
            }
        }



        public static void ReplicateCommands(List<string> commands)
        {
            foreach( string cmd in commands) executeCommands(cmd);
        }

        public static bool executeCommands(String key)
        {
            Console.WriteLine("COMANDO KEY = " + key);

            if (commands.ContainsKey(key))
            {
                if (commands[key].Split(" ")[0] == "D")
                {

                    account.Deposit(int.Parse(commands[key].Split(" ")[1]));
                    commands.Remove(key);
                    if(primary) replicateCommands.Add(key);
                    return true;
                }
                else if (commands[key].Split(" ")[0] == "W")
                {
                    bool ok  = account.Withdrawal(int.Parse(commands[key].Split(" ")[1]));
                    commands.Remove(key);
                    if(primary) replicateCommands.Add(key);
                    return ok;

                }
            }
            return true;
        }
    }

    public class BankServerInterceptor : Interceptor
    {
        public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
        {
            string callId = context.RequestHeaders.GetValue("dad");
            //Console.WriteLine("DAD header: " + callId);
            return base.UnaryServerHandler(request, context, continuation);
        }
        public override TResponse BlockingUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
        {

            Metadata metadata = context.Options.Headers; // read original headers
            if (metadata == null) { metadata = new Metadata(); }
            metadata.Add("dad", "dad-value"); // add the additional metadata

            // create new context because original context is readonly
            ClientInterceptorContext<TRequest, TResponse> modifiedContext =
                new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host,
                    new CallOptions(metadata, context.Options.Deadline,
                        context.Options.CancellationToken, context.Options.WriteOptions,
                        context.Options.PropagationToken, context.Options.Credentials));
            //Console.Write("calling server...");
            TResponse response = base.BlockingUnaryCall(request, modifiedContext, continuation);
            return response;
        }
    }
}
