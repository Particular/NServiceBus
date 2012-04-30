using NServiceBus.Unicast.Queuing;

namespace NServiceBus.Timeout.Hosting.Windows
{
    using System;
    using Core;
    using Faults;
    using ObjectBuilder;
    using Unicast;
    using Unicast.Queuing.Msmq;
    using Unicast.Transport;
    using Unicast.Transport.Transactional;

    public class TimeoutMessageProcessor : IWantToRunWhenTheBusStarts,IDisposable 
    {
        public TransactionalTransport MainTransport { get; set; }

        public IBuilder Builder { get; set; }

        public static Func<IReceiveMessages> MessageReceiverFactory { get; set; }

        public void Run()
        {

            if (!Configure.Instance.IsTimeoutManagerEnabled())
                return;

            var messageReceiver = MessageReceiverFactory != null ? MessageReceiverFactory() : new MsmqMessageReceiver();

            inputTransport = new TransactionalTransport
            {
                MessageReceiver = messageReceiver,
                IsTransactional = true,
                NumberOfWorkerThreads = MainTransport.NumberOfWorkerThreads == 0 ? 1 : MainTransport.NumberOfWorkerThreads,
                MaxRetries = MainTransport.MaxRetries,
                FailureManager = Builder.Build(MainTransport.FailureManager.GetType())as IManageMessageFailures
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