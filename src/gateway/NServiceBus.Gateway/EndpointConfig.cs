using System.Configuration;
using System.Net;
using System.Threading;
using NServiceBus.Unicast.Transport;
using NServiceBus.Unicast.Transport.Msmq;

namespace NServiceBus.Gateway
{
    public class EndpointConfig : IConfigureThisEndpoint, IWantCustomInitialization, IWantToRunAtStartup
    {
        public void Init()
        {
            NServiceBus.Configure.With().DefaultBuilder();

            string inputQueue = ConfigurationManager.AppSettings["InputQueue"];
            string errorQueue = ConfigurationManager.AppSettings["ErrorQueue"];
            string audit = ConfigurationManager.AppSettings["ForwardReceivedMessageTo"];
            string listenUrl = ConfigurationManager.AppSettings["ListenUrl"];
            string n = ConfigurationManager.AppSettings["NumberOfWorkerThreads"];
            string req = ConfigurationManager.AppSettings["RequireMD5FromClient"];
            
            outputQueue = ConfigurationManager.AppSettings["OutputQueue"];

            int numberOfWorkerThreads = 10;
            int.TryParse(n, out numberOfWorkerThreads);

            bool.TryParse(req, out requireMD5FromClient);

            ThreadPool.SetMaxThreads(numberOfWorkerThreads, numberOfWorkerThreads);

            transport = new MsmqTransport
                                {
                                    InputQueue = inputQueue,
                                    ErrorQueue = errorQueue,
                                    IsTransactional = true,
                                    SkipDeserialization = true,
                                    NumberOfWorkerThreads = numberOfWorkerThreads
                                };

            notifier = new MessageNotifier();

            NServiceBus.Configure.Instance.Configurer.RegisterSingleton<ITransport>(transport);
            NServiceBus.Configure.Instance.Configurer.RegisterSingleton<INotifyAboutMessages>(notifier);

            transport.TransportMessageReceived += (s, e) => 
                {
                    new MsmqHandler(listenUrl, notifier).Handle(e.Message);

                    if (!string.IsNullOrEmpty(audit))
                        transport.Send(e.Message, audit);
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
                    o => new HttpRequestHandler(requireMD5FromClient, notifier).Handle(o as HttpListenerContext, transport, outputQueue),
                    context);
            }
        }

        public void Stop()
        {
            stopRequested = true;
        }

        private static HttpListener listener;
        private static MsmqTransport transport;
        private static MessageNotifier notifier;
        private static bool requireMD5FromClient = true;
        private static string outputQueue;

        private volatile bool stopRequested;
    }
}
