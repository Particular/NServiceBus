using System;
using System.Configuration;
using log4net;
using NServiceBus.Unicast.Transport.Msmq;
using System.Net;
using System.Threading;
using NServiceBus.Gateway;

namespace Gateway.Host
{
    class Program
    {
        static void Main()
        {
            string inputQueue = ConfigurationManager.AppSettings["InputQueue"];
            string outputQueue = ConfigurationManager.AppSettings["OutputQueue"];
            string listenUrl = ConfigurationManager.AppSettings["ListenUrl"];
            string remoteUrl = ConfigurationManager.AppSettings["RemoteUrl"];

            Logger.Info("Listening on queue: " + inputQueue);
            Logger.Info("Listening on url: " + listenUrl);

            var transport = new MsmqTransport
                                {
                                    InputQueue = inputQueue,
                                    IsTransactional = true,
                                    SkipDeserialization = true,
                                    NumberOfWorkerThreads = 1
                                };

            transport.TransportMessageReceived += (s, e) => MsmqHandler.Handle(e.Message, remoteUrl);

            transport.Start();

            var listener = new HttpListener();
            listener.Prefixes.Add(listenUrl);
            listener.Start();

            while (true)
            {
                HttpListenerContext context = listener.GetContext();
                new Thread(o => HttpRequestHandler.Handle(((HttpListenerContext)o).AsIContext(), transport, outputQueue))
                    .Start(context);
            }
// ReSharper disable FunctionNeverReturns
        }
// ReSharper restore FunctionNeverReturns

        private static readonly ILog Logger = LogManager.GetLogger(typeof(Program));
    }
}
