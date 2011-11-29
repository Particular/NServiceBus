namespace NServiceBus.Timeout.Hosting.Windows
{
    using System;
    using Config;
    using Core;
    using ObjectBuilder;
    using Unicast.Queuing.Msmq;
    using Unicast.Transport;
    using Unicast.Transport.Transactional;

    public class TimeoutManagerBootstrapper : IDisposable, IWantToRunWhenConfigurationIsComplete
    {
        public TransactionalTransport MainTransport { get; set; }
        
        public TimeoutRunner Runner { get; set; }

        public IStartableBus Bus { get; set; }

        public IBuilder Builder { get; set; }

        
        public void Run()
        {
            if (!ConfigureTimeoutManager.TimeoutManagerEnabled)
                return;

            inputTransport = new TransactionalTransport
            {
                MessageReceiver = new MsmqMessageReceiver(),
                IsTransactional = true,
                NumberOfWorkerThreads = MainTransport.NumberOfWorkerThreads == 0 ? 1 : MainTransport.NumberOfWorkerThreads,
                MaxRetries = MainTransport.MaxRetries,
                FailureManager = MainTransport.FailureManager
            };

            inputTransport.TransportMessageReceived += OnTransportMessageReceived;


         
            Bus.Started += (obj, ev) =>
                               {
                                   Runner.Run();
                                   inputTransport.Start(ConfigureTimeoutManager.TimeoutManagerAddress);
                               };
        }

        void OnTransportMessageReceived(object sender, TransportMessageReceivedEventArgs e)
        {
            Builder.Build<TimeoutTransportMessageHandler>()
                .Handle(e.Message);
        }


        ITransport inputTransport;

        public void Dispose()
        {
            if (inputTransport != null)
                inputTransport.Dispose();
        }

    }
}