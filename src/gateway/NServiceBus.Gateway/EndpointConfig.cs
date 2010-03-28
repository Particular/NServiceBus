using System.Configuration;
using System.Net;
using System.Threading;
using NServiceBus.Unicast.Queuing.Msmq;
using NServiceBus.Unicast.Transport.Msmq;

namespace NServiceBus.Gateway
{
    public class EndpointConfig : IConfigureThisEndpoint, IWantCustomInitialization, IWantToRunAtStartup
    {
        public void Init()
        {
            NServiceBus.Configure.With().DefaultBuilder();

            string errorQueue = ConfigurationManager.AppSettings["ErrorQueue"];
            string audit = ConfigurationManager.AppSettings["ForwardReceivedMessageTo"];
            string listenUrl = ConfigurationManager.AppSettings["ListenUrl"];
            string n = ConfigurationManager.AppSettings["NumberOfWorkerThreads"];
            string req = ConfigurationManager.AppSettings["RequireMD5FromClient"];
            string remoteUrl = ConfigurationManager.AppSettings["RemoteUrl"];

            inputQueue = ConfigurationManager.AppSettings["InputQueue"];
            outputQueue = ConfigurationManager.AppSettings["OutputQueue"];

            int numberOfWorkerThreads = 10;
            int.TryParse(n, out numberOfWorkerThreads);

            bool.TryParse(req, out requireMD5FromClient);

            ThreadPool.SetMaxThreads(numberOfWorkerThreads, numberOfWorkerThreads);

            sender = new MsmqMessageQueue();
            sender.Init(inputQueue);

            transport = new MsmqTransport
            {
                MessageQueue = sender,
                IsTransactional = true,
                NumberOfWorkerThreads = numberOfWorkerThreads
            };

            transport.TransportMessageReceived += (s, e) =>
            {
                new MsmqHandler(listenUrl).Handle(e.Message, remoteUrl);

                if (!string.IsNullOrEmpty(audit))
                    sender.Send(e.Message, audit);
            };

            listener = new HttpListener();
            listener.Prefixes.Add(listenUrl);
        }

        public void Run()
        {
            transport.Start();
            listener.Start();

            while (!stopRequested)
            {
                HttpListenerContext context = listener.GetContext();
                ThreadPool.QueueUserWorkItem(
                    o => new HttpRequestHandler(requireMD5FromClient, inputQueue).Handle(o as HttpListenerContext, sender, outputQueue),
                    context);
            }
        }

        public void Stop()
        {
            stopRequested = true;
        }

        private static HttpListener listener;
        private static MsmqTransport transport;
        private static MsmqMessageQueue sender;
        private static bool requireMD5FromClient = true;
        private static string outputQueue;
        private static string inputQueue;

        private volatile bool stopRequested;
    }
}
