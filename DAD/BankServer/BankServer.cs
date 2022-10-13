using System.Collections.Generic;
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
        private static int port;
        private static bool primary = false;
        private static List<int> banksID = new List<int>();
        private static BankAccount account = new BankAccount();
        private static bool frozen = false;
        private static readonly object frozenLock = new object();
        private static Dictionary<string, BankToBankService.BankToBankServiceClient> otherBankServers = new Dictionary<string, BankToBankService.BankToBankServiceClient>();
        private static Dictionary<string, BankPaxosService.BankPaxosServiceClient> PaxosServers = new Dictionary<string, BankPaxosService.BankPaxosServiceClient>();
        private static Dictionary<string, string> commands = new Dictionary<string, string>();
        static void Main(string[] args)
        {
            bool keepRunning = true;
            id = Int16.Parse(args[0]);
            port = Int16.Parse(args[1]);
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


            //GrpcChannel channel = GrpcChannel.ForAddress("http://" + ServerHostname + ":" + PaxosPort);
            //CallInvoker interceptingInvoker = channel.Intercept(new BankServerInterceptor());
            //var paxosServer = new BankPaxosService.BankPaxosServiceClient(interceptingInvoker);

            createChannels(args);
            GreetBankServers();


            /*var timer = new System.Timers.Timer(TimeSpan.FromSeconds(10).TotalMilliseconds);

            timer.AutoReset = true;
            timer.Elapsed += PrimaryElection;
            timer.Start();*/


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
        private static void createChannels(string[] args)
        {
            for (int i = 2; i < args.Length; i++)
            {
                if (i < 4)
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
            if (id == 4)
            {
                foreach (KeyValuePair<string, BankPaxosService.BankPaxosServiceClient> paxosserver in PaxosServers)
                {

                    GreetReply3 reply = paxosserver.Value.Greeting(new GreetRequest3 { Hi = true });
                    Console.WriteLine(reply.Hi.ToString() + "\r\n");

                }
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

        public static Dictionary<string ,BankToBankService.BankToBankServiceClient> getOtherBankServers() { return otherBankServers; }

        public static void Replica (string key)
        {
            ReplicaRequest request = new ReplicaRequest { Key = key };
            foreach (KeyValuePair<string, BankToBankService.BankToBankServiceClient> server in otherBankServers)
            {
                server.Value.Replica(request);
            }
        }



        //EM VEZ DE FAZER COMANDO A COMANDO ENVIAR APENAS O BALANCE ATULIZADO
        //CRIAR FINCAP SET BALANCE NA CLASSE BANK ACCOUNT E QUANDO RECEBE O VALOR ATUALIZA

        public static void executeCommands(String key)
        {

            //account.setValue(balance);
            Console.WriteLine("key = " + key);
            key = "7 1";
            Console.WriteLine(commands.ContainsKey(key));
            if (commands.ContainsKey(key))  
            {
                if (commands[key].Split(" ")[0].Equals("D"))
                {
                    Console.WriteLine(commands[key].Split(" ")[1]);
                    account.Deposit(Double.Parse(commands[key].Split(" ")[1]));
                    commands.Remove(key);
                }
                if (commands[key].Split(" ")[0].Equals("W"))
                {
                    account.Withdrawal(Double.Parse(commands[key].Split(" ")[1]));
                    commands.Remove(key);
                }

            }
            Console.WriteLine("COMANDOS DEPOIS DA REPLICA:");
            foreach(KeyValuePair<string,string> kvp in commands)
            {
                Console.WriteLine(kvp.Key + "=" + kvp.Value );
            }
        }
    }


    //aaa

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
