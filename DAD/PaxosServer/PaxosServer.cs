using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;

namespace PaxosServer
{
    public class PaxosServer
    {
        private static string[] configurationText = File.ReadAllLines("configuration_sample.txt");
        private static int id;
        private static int Port;
        private static Paxos paxos;
        private static Dictionary<string, PaxosToPaxosService.PaxosToPaxosServiceClient> otherPaxosServers = new Dictionary<string, PaxosToPaxosService.PaxosToPaxosServiceClient>();
        private static bool frozen = false;
        private static object frozenLock = new object();
        private static List<int> paxosServersID = new List<int>();
        private static int _slot;

        static void Main(string[] args)
        {
            id = Int16.Parse(args[0]);
            paxos = new Paxos(Int16.Parse(args[0]));
            Port = Int16.Parse(args[1]);
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            bool keepRunning = true;

            Server server = new Server
            {
                Services = { BankPaxosService.BindService(new BankService()).Intercept(new PaxosServerInterceptor()),  
                PaxosToPaxosService.BindService(new PaxosPaxosService(paxos)).Intercept(new PaxosServerInterceptor())},
                Ports = { new ServerPort("localhost", Port, ServerCredentials.Insecure) }
            };
            server.Start();
            Console.WriteLine("PaxosServer server listening on port " + Port);

            createChannels(args);
            GreetPaxosServers();

            Console.WriteLine("Press any key to stop the server...");

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

        public static bool GetFrozen()
        {
            lock (frozenLock)
            {
                return frozen;
            }
        }

        public static int getId() { return id; }
      
        public static void update_slot(int round)
        {
            _slot = round;
        }

        public static async Task<int> Paxos(int value, int slot)
        {
            if (readConfigurationLines(slot))
            {
                Console.WriteLine("Vou avançar com o Paxos");
                int valueToPropose = await doPrepare(value, slot);
                if (valueToPropose == 0) { return 0; }
                int valueAccepted = await doAccept(id, valueToPropose);
                if (valueAccepted == 0) { return 0; }
                if (await doCommit(valueAccepted, slot)) { return valueAccepted; }
            }
            return 0;
        }

        private static async Task<int> doPrepare(int numberToPropose, int slot)
        {
            int count = 1;
            int number_to_propose = numberToPropose;
            foreach (KeyValuePair<string, PaxosToPaxosService.PaxosToPaxosServiceClient> paxosserver in otherPaxosServers)
            {
                try
                {
                    Promise reply = await (paxosserver.Value.PrepareAsync(new PrepareRequest { ProposerID = id, Slot = slot }, deadline: DateTime.UtcNow.AddSeconds(1)));
                    if (reply.Value.Count() == 0) //recebeu uma lista vazia logo o read_ts apresenta um numero maior que o que ele tentou. Vai agora tentar com um maior
                    {
                        return 0;
                    }
                    else //como ninguem com o id superior ao dele propos nada ele vai fazer o accept com o valor que enviou
                    {
                        if (reply.Value[0] == 0) { count++; } //fazer accept com o id do bank que recebemos para fazer paxos
                        else
                        {
                            count++; 
                            number_to_propose = reply.Value[1];
                        }
                    }
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded) { continue; };
            }
            if (count > paxosServersID.Count()/2)
            {
                return number_to_propose; 
            }
            return 0;
        }

        private static async Task<int> doAccept(int id, int value_to_accept)
        {
            int count = 1;
            foreach (KeyValuePair<string, PaxosToPaxosService.PaxosToPaxosServiceClient> paxosserver in otherPaxosServers)
            {
                try
                {
                    Accepted_message reply_accept = await (paxosserver.Value.AcceptRequestAsync(new Accept { ProposerID = id, Value = value_to_accept }, deadline: DateTime.UtcNow.AddSeconds(1)));
                    if (reply_accept.ValuePromised == value_to_accept && reply_accept.ProposerID == id)
                    {
                        count++;
                    }
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded) { continue; };
            }
            if (count > otherPaxosServers.Count() / 2)
            {
                return value_to_accept;
            }
            return 0;
        }

        private static async Task<bool> doCommit(int value_to_commit, int slot)
        {
            int count = 1;
            foreach (KeyValuePair<string, PaxosToPaxosService.PaxosToPaxosServiceClient> paxosserver in otherPaxosServers)
            {
                try
                {
                    CommitReply commit = await (paxosserver.Value.CommitAsync(new CommitRequest { Slot = slot, Value = value_to_commit }, deadline: DateTime.UtcNow.AddSeconds(1)));
                    count++;
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded) { continue; };
            }
            if (count > otherPaxosServers.Count() / 2)
            {
                return true;
            }
            return false;
        }

        private static bool readConfigurationLines(int slot)
        {
            int i = 0;
            int leader = 0;
            foreach (string line in configurationText)
            {
                if (line[0] == 'F' && Int32.Parse(line.Split(" ")[1]) == slot)
                {
                    foreach (int e in paxosServersID)
                    {
                        if (e < id)
                        {
                            string tuplo = line.Split(")")[i++].Substring(1);
                            string frozen;
                            if (i == 1)
                            {
                                frozen = tuplo.Split(" ")[4];

                            }
                            else 
                            {
                                frozen = tuplo.Split(" ")[2];
                            }
                            if (frozen == "S")
                            {
                                continue;
                            }
                            else
                            {
                                leader = e;
                                return leader == id;
                            }
                        }
                        else if (e == id)
                        {
                            leader = id;
                            return leader == id;
                        }
                    }
                }
            }
            leader = paxosServersID.Min();
            return leader == id;
        }

        public static void isFrozen()
        {
            int i = 0;
            string froz;
            foreach (string line in configurationText)
            {
                if (line[0] == 'F' && Int32.Parse(line.Split(" ")[1]) == _slot) //selects the line
                {
                    foreach (int paxos in paxosServersID)
                    {
                        if (paxos == id)
                        {
                            string tuplo = line.Split(")")[i].Substring(1);
                            if (i == 0)
                            {
                                froz = tuplo.Split(" ")[3];
                            }
                            else
                            {
                                froz = tuplo.Split(" ")[1];
                            }
                            froz = froz.Remove(froz.Length - 1, 1);
                            if (froz == "F")
                            {
                                lock(frozenLock) 
                                {
                                    frozen = true;
                                }
                                return;
                            }
                            else
                            {
                                lock (frozenLock)
                                {
                                    frozen = false;
                                }
                                return;
                            }
                        }
                        i++;
                    }
                }
            }
            frozen = false;
        }


        private static void createChannels(string[] args)
        {
            for (int i = 2; i < args.Length; i++)
            {
                GrpcChannel channel = GrpcChannel.ForAddress("http://localhost:" + args[i]);
                CallInvoker interceptingInvoker = channel.Intercept(new PaxosServerInterceptor());
                PaxosToPaxosService.PaxosToPaxosServiceClient server = new PaxosToPaxosService.PaxosToPaxosServiceClient(interceptingInvoker);
                string host = "http://localhost:" + args[i];
                otherPaxosServers.Add(host, server);
            }
        }
        public static int getID()
        {
            return id;
        }

        private static void GreetPaxosServers()
        {
            paxosServersID.Add(id);
            foreach (KeyValuePair<string, PaxosToPaxosService.PaxosToPaxosServiceClient> server in otherPaxosServers)
            {
                GreetReply reply = server.Value.Greeting(new GreetRequest { Id = id });
                paxosServersID.Add(reply.Id);
            }
            paxosServersID.Sort();
        }
    }
}

public class PaxosServerInterceptor : Interceptor
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
