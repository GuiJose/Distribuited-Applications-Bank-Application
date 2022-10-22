using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using System.Timers;

namespace PaxosServer
{
    public class PaxosServer
    {
        private static System.Timers.Timer timerHello = new System.Timers.Timer(TimeSpan.FromSeconds(10).TotalMilliseconds);
        private static System.Timers.Timer timer1 = new System.Timers.Timer(TimeSpan.FromSeconds(30).TotalMilliseconds);
        private static System.Timers.Timer timer2 = new System.Timers.Timer(TimeSpan.FromSeconds(30).TotalMilliseconds);
        private static System.Timers.Timer timer3 = new System.Timers.Timer(TimeSpan.FromSeconds(30).TotalMilliseconds);
        private static bool is1Frozen = false;
        private static bool is2Frozen = false;
        private static bool is3Frozen = false;
        private static int currentLeader = 1;
        private static int nextNumberToUse;
        private static int id;
        private static int Port;
        private static Paxos paxos;
        private static Dictionary<string, PaxosToPaxosService.PaxosToPaxosServiceClient> otherPaxosServers = new Dictionary<string, PaxosToPaxosService.PaxosToPaxosServiceClient>();

        static void Main(string[] args)
        {
            id = Int16.Parse(args[0]);
            nextNumberToUse = id;
            paxos = new Paxos(Int16.Parse(args[0]));
            Port = Int16.Parse(args[1]);
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            Server server = new Server
            {
                Services = { BankPaxosService.BindService(new BankService()).Intercept(new PaxosServerInterceptor()),  
                PaxosToPaxosService.BindService(new PaxosPaxosService(paxos)).Intercept(new PaxosServerInterceptor())},
                Ports = { new ServerPort("localhost", Port, ServerCredentials.Insecure) }
            };
            server.Start();
            Console.WriteLine("PaxosServer server listening on port " + Port);

            createChannels(args);

            //Começar processo Paxos
            /* Esqueleto:
             processo_paxos(){
                PREPARE_PROMISSE();
                VERIFICAÇÕES
                ACCEPT_ACCEPTED();
                FOI ACEITE?
                DEVOLVER RESPOSTA AO BANCO
            }
            */
            //Quando recebe mensagem dos bancos executa este codigo a baixo (transformar em função)

            //Fazer accept
            timerHello.AutoReset = true;
            timerHello.Elapsed += sendHello;
            timerHello.Start();
            timer1.AutoReset = true;
            timer1.Elapsed += setFrozen1;
            timer1.Start();
            timer2.AutoReset = true;
            timer2.Elapsed += setFrozen2;
            timer2.Start();
            timer3.AutoReset = true;
            timer3.Elapsed += setFrozen3;
            timer3.Start();

            Console.WriteLine("Press any key to stop the server...");
            Console.ReadKey();
            server.ShutdownAsync().Wait();
        }

        private static void setFrozen1(object sender, ElapsedEventArgs e)
        {
            is1Frozen = true;
            Console.WriteLine("o 1 está parado");
            if (currentLeader%3 == 1 && id == 2)
            {
                doPrepare();
            }
        }
        private static void setFrozen2(object sender, ElapsedEventArgs e)
        {
            is2Frozen = true;
            Console.WriteLine("o 2 está parado");
        }
        private static void setFrozen3(object sender, ElapsedEventArgs e)
        {
            is3Frozen = true;
            Console.WriteLine("o 3 está parado");
        }

        public static void setFrozenFalse(int id)
        {
            if (id == 1)
            {
                is1Frozen = false;
            }
            else if (id == 2)
            {
                is2Frozen = false;
            }
            else
            {
                is3Frozen = false;
            }
        }


        private static void sendHello(object sender, ElapsedEventArgs e)
        {
            foreach (KeyValuePair<string, PaxosToPaxosService.PaxosToPaxosServiceClient> paxosserver in otherPaxosServers)
            {
               paxosserver.Value.AliveAsync(new AliveRequest { Id = id });
            }
        }

        private static void doPrepare()
        {
            foreach (KeyValuePair<string, PaxosToPaxosService.PaxosToPaxosServiceClient> paxosserver in otherPaxosServers)
            {
                paxosserver.Value.Prepare(new PrepareRequest { ProposerID = id });
            }

            nextNumberToUse += 3;
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

        public static void resetTimer(int i)
        {
            if (i == 1)
            {
                timer1.Stop();
                timer1.Start();
            }
            else if (i == 2)
            {
                timer2.Stop();
                timer2.Start();
            }
            else
            {
                timer3.Stop();
                timer3.Start();
            }
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
