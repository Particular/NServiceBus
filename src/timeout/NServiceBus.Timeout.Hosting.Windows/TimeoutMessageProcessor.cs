namespace NServiceBus.Timeout.Hosting.Windows
{
    using System;
    using Config;
    using Core;
    using ObjectBuilder;
    using Unicast;
    using Unicast.Queuing.Msmq;
    using Unicast.Transport;
    using Unicast.Transport.Transactional;

    public class TimeoutMessageProcessor : IWantToRunWhenTheBusStarts,IDisposable 
    {
        public TransactionalTransport MainTransport { get; set; }
        
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

            inputTransport.Start(ConfigureTimeoutManager.TimeoutManagerAddress);
        }

        void OnTransportMessageReceived(object sender, TransportMessageReceivedEventArgs e)
        {
            Builder.Build<TimeoutTransportMessageHandler>()
                .Handle(e.Message);
        }


     
        public void Dispose()
        {
            if (inputTransport != null)
                inputTransport.Dispose();
        }


        ITransport inputTransport;
    }
}