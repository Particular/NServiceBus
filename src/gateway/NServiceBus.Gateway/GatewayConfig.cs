namespace NServiceBus.Gateway
{
    using System.Configuration;
    using System.Net;
    using System.Threading;
    using ObjectBuilder;
    using Persistence;
    using Unicast.Queuing;
    using Unicast.Queuing.Msmq;

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
            var inputQueue = ConfigurationManager.AppSettings["InputQueue"];
            var outputQueue = ConfigurationManager.AppSettings["OutputQueue"];
            var connectionString = ConfigurationManager.AppSettings["ConnectionString"];
         


            int numberOfWorkerThreads;

            if (!int.TryParse(n, out numberOfWorkerThreads))
                numberOfWorkerThreads = 10;

            ThreadPool.SetMaxThreads(numberOfWorkerThreads, numberOfWorkerThreads);

            //todo, use the one from the main bus
            var messageSender = new MsmqMessageSender { UseDeadLetterQueue = true, UseJournalQueue = true };


            var notifier = new MessageNotifier();

            Configure.Instance.Configurer.RegisterSingleton<ISendMessages>(messageSender);
            Configure.Instance.Configurer.RegisterSingleton<INotifyAboutMessages>(notifier);
            Configure.Instance.Configurer.RegisterSingleton<IMessageNotifier>(notifier);

            Configure.Instance.Configurer.ConfigureComponent<GatewayService>(DependencyLifecycle.SingleInstance)
               .ConfigureProperty(p => p.DestinationAddress, outputQueue);
          
            Configure.Instance.Configurer.ConfigureComponent<SqlPersistence>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.ConnectionString, connectionString);

            Configure.Instance.Configurer.ConfigureComponent<HttpChannel>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.ListenUrl, listenUrl)
                .ConfigureProperty(p => p.ReturnAddress, inputQueue);
      
            Configure.Instance.Configurer.ConfigureComponent<MsmqInputDispatcher>(DependencyLifecycle.SingleInstance)
             .ConfigureProperty(p => p.NumberOfWorkerThreads, numberOfWorkerThreads)
             .ConfigureProperty(p => p.InputQueue, inputQueue)
             .ConfigureProperty(p => p.RemoteAddress, remoteUrl);
        }


    }
}