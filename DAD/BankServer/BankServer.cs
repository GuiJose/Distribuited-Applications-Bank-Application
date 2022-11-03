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
        private static Dictionary<string, BankToBankService.BankToBankServiceClient> otherBankServers = new Dictionary<string, BankToBankService.BankToBankServiceClient>();
        private static Dictionary<string, BankPaxosService.BankPaxosServiceClient> PaxosServers = new Dictionary<string, BankPaxosService.BankPaxosServiceClient>();
        private static Dictionary<string, string> commands = new Dictionary<string, string>();
        private static List<String> replicateCommands = new List<string>();
        private static string[] configurationText = File.ReadAllLines("configuration_sample.txt");
        private static int current_lider;
        private static int numberSlots = readNumberSlots();
        private static object frozenObject = new object();

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
            int slotDuration = readSlotDuration();

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

            var paxos = new System.Timers.Timer(TimeSpan.FromSeconds(slotDuration).TotalMilliseconds);

            paxos.AutoReset = true;
            paxos.Elapsed += PrimaryElection;
            paxos.Start();

            var replica = new System.Timers.Timer(TimeSpan.FromSeconds(10).TotalMilliseconds);

            replica.AutoReset = true;
            replica.Elapsed += Replica;
            replica.Start();

            while (keepRunning)
            {
                Console.WriteLine("Press 'F' to frozen the process, 'N' to put the process in its normal condition or press" +
                    "'X' to finish the process.\r\n");
                switch (Console.ReadKey().Key)
                {
                    case ConsoleKey.F:
                            frozen = true;
                        break;
                    case ConsoleKey.N:
                            frozen = false;
                        break;

                    case ConsoleKey.X:
                        keepRunning = false;
                        break;

                }
            }
            server.ShutdownAsync().Wait();
        }

        public static object getFrozenObject() { return frozenObject; }

        public static bool GetFrozen() { return frozen; }
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

        private static void isFrozen()
        {
            int i = PaxosServers.Count();
            foreach (string line in configurationText)
            {
                if (line[0] == 'F' && Int32.Parse(line.Split(" ")[1]) == slot) //selects the line
                {
                    foreach (int bank in banksID)
                    {
                        if (bank == id)
                        {
                            string tuplo = line.Split(")")[i].Substring(1);
                            string froz = tuplo.Split(" ")[1];
                            froz = froz.Remove(froz.Length - 1, 1);
                            if (froz == "F")
                            {
                                frozen = true;
                            }
                            else
                            {
                                frozen = false;
                                //Monitor.PulseAll(frozenObject);
                            }
                        }
                        i++;
                    }
                }
            }
        }

        private static int readSlotDuration()
        {
            foreach (string line in configurationText)
            {
                if (line[0] == 'D')
                {
                    return Int32.Parse(line.Split(" ")[1]);
                }
            }
            return 30;
        }

        private static int readNumberSlots()
        {
            foreach (string line in configurationText)
            {
                if (line[0] == 'S')
                {
                    return Int32.Parse(line.Split(" ")[1]);
                }
            }
            return 0;
        }

        private static int decider()//vai receber o id do processo, e id ultimo lider
        {
            int i = PaxosServers.Count();
            if (current_lider == id)
            {
                return id;
            }
            foreach (string line in configurationText)
            {
                if (line[0] == 'F' && Int32.Parse(line.Split(" ")[1]) == slot) //selects the line
                {
                    foreach (int bank in banksID)
                    {
                        if (current_lider == bank)
                        {
                            string tuplo = line.Split(")")[i].Substring(1);
                            string sus = tuplo.Split(" ")[2];
                            if (sus == "S")
                            {
                                return id;
                            }
                            else //case of being NS
                            {
                                return current_lider;
                            }
                        }
                        i++;
                    }
                }
            }
            return id;
        }

        private static void PrimaryElection(object sender, ElapsedEventArgs e)
        {
            if (slot > numberSlots)
            {
                return;
            }
            isFrozen();
            Console.WriteLine("FROZEN? = " + frozen);
            Console.WriteLine("ENVIEI PEDIDO DE PAXOS COM ID = " + decider());
            foreach (KeyValuePair<string, BankPaxosService.BankPaxosServiceClient> paxosserver in PaxosServers)
            {
                CompareAndSwapReply reply = paxosserver.Value.CompareAndSwap(new CompareAndSwapRequest { Value = decider(), Slot = slot });
                if (reply.Value == 0)
                {
                    continue;
                }
                current_lider = reply.Value;
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
            if (primary)
            {
                ReplicateCommands(commands.Keys.ToList());
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
            int minId = id;

            foreach (KeyValuePair<string, BankToBankService.BankToBankServiceClient> server in otherBankServers)
            {
                GreetReply reply = server.Value.Greeting(new GreetRequest { Id = id });
                banksID.Add(reply.Id);
                if (reply.Id < minId) minId = reply.Id;
            }
            current_lider = minId;
            if (id == minId) setPrimary(true);
            banksID.Sort();
        }
        
        public static int getId() { return id; }

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
                    account.Deposit(Convert.ToDouble(commands[key].Split(" ")[1]));
                    commands.Remove(key);
                    if(primary) replicateCommands.Add(key);
                    return true;
                }
                else if (commands[key].Split(" ")[0] == "W")
                {
                    bool ok  = account.Withdrawal(Convert.ToDouble(commands[key].Split(" ")[1]));
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
