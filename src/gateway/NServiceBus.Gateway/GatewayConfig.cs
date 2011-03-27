namespace NServiceBus.Gateway
{
    using System.Configuration;
    using System.Net;
    using System.Threading;
    using Unicast.Queuing;
    using Unicast.Queuing.Msmq;
    using Unicast.Transport;
    using Unicast.Transport.Transactional;

    public class GatewayConfig : IWantCustomInitialization
    {
        public void Init()
        {
            //should we be using this or will the faultmanager cover this? (ie should we pick up the faultmanager from the main bus instance)
            string errorQueue = ConfigurationManager.AppSettings["ErrorQueue"];
            string audit = ConfigurationManager.AppSettings["ForwardReceivedMessageTo"];
            string listenUrl = ConfigurationManager.AppSettings["ListenUrl"];
            string n = ConfigurationManager.AppSettings["NumberOfWorkerThreads"];
            string remoteUrl = ConfigurationManager.AppSettings["RemoteUrl"];


            int numberOfWorkerThreads;

            if (!int.TryParse(n, out numberOfWorkerThreads))
                numberOfWorkerThreads = 10;

            ThreadPool.SetMaxThreads(numberOfWorkerThreads, numberOfWorkerThreads);


            var messageSender = new MsmqMessageSender { UseDeadLetterQueue = true, UseJournalQueue = true };

            var transport = new TransactionalTransport
                                {
                                    MessageReceiver = new MsmqMessageReceiver(),
                                    IsTransactional = true,
                                    NumberOfWorkerThreads = numberOfWorkerThreads
                                };

            var notifier = new MessageNotifier();

            Configure.Instance.Configurer.RegisterSingleton<ISendMessages>(messageSender);
            Configure.Instance.Configurer.RegisterSingleton<ITransport>(transport);
            Configure.Instance.Configurer.RegisterSingleton<INotifyAboutMessages>(notifier);
            Configure.Instance.Configurer.RegisterSingleton<IMessageNotifier>(notifier);

            transport.TransportMessageReceived += (s, e) =>
                {
                    new HttpSender(notifier, listenUrl).Send(e.Message, remoteUrl);


                    //todo get audit settings from the audit settings of the host (possibly allowing to override in config)
                    if (!string.IsNullOrEmpty(audit))
                        messageSender.Send(e.Message, audit);
                };

            var listener = new HttpListener();
            listener.Prefixes.Add(listenUrl);

            Configure.Instance.Configurer.RegisterSingleton<HttpListener>(listener);
        }
    }
}