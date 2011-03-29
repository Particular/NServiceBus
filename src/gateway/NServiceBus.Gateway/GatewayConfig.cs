namespace NServiceBus.Gateway
{
    using System;
    using System.Configuration;
    using System.Net;
    using System.Threading;
    using Hosting.Profiles;
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



            int numberOfWorkerThreads;

            if (!int.TryParse(n, out numberOfWorkerThreads))
                numberOfWorkerThreads = 10;


            //todo, use the one from the main bus
            var messageSender = new MsmqMessageSender { UseDeadLetterQueue = true, UseJournalQueue = true };


            var notifier = new MessageNotifier();

            Configure.Instance.Configurer.RegisterSingleton<ISendMessages>(messageSender);
            Configure.Instance.Configurer.RegisterSingleton<INotifyAboutMessages>(notifier);
            Configure.Instance.Configurer.RegisterSingleton<IMessageNotifier>(notifier);

            Configure.Instance.Configurer.ConfigureComponent<GatewayService>(DependencyLifecycle.SingleInstance)
               .ConfigureProperty(p => p.DefaultDestinationAddress, outputQueue);


            Configure.Instance.Configurer.ConfigureComponent<HttpChannel>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.ListenUrl, listenUrl)
                .ConfigureProperty(p => p.ReturnAddress, inputQueue)
                .ConfigureProperty(p => p.NumberOfWorkerThreads, numberOfWorkerThreads);

            Configure.Instance.Configurer.ConfigureComponent<MsmqInputDispatcher>(DependencyLifecycle.SingleInstance)
             .ConfigureProperty(p => p.InputQueue, inputQueue)
             .ConfigureProperty(p => p.RemoteAddress, remoteUrl);
        }


    }

    internal class LiteProfileHandler : IHandleProfile<Lite>
    {
        public void ProfileActivated()
        {
            Configure.Instance.Configurer.ConfigureComponent<InMemoryPersistence>(DependencyLifecycle.SingleInstance);
        }
    }

    //todo
    //internal class IntegrationProfileHandler : IHandleProfile<Integration>
    //{
    //    public void ProfileActivated()
    //    {
    //        var connectionString = ConfigurationManager.AppSettings["ConnectionString"];

    //        Configure.Instance.Configurer.ConfigureComponent<SqlPersistence>(DependencyLifecycle.InstancePerCall)
    //         .ConfigureProperty(p => p.ConnectionString, connectionString);

    //    }
    //}

}