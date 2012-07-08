namespace NServiceBus.Timeout.Hosting.Windows
{
    using System;
    using Core;
    using Core.Dispatch;
    using Faults;
    using ObjectBuilder;
    using Unicast.Queuing.Msmq;
    using Unicast.Transport;
    using Unicast.Transport.Transactional;
    using NServiceBus.Unicast.Queuing;

    public class TimeoutMessageProcessor : IWantToRunWhenBusStartsAndStops
    {
        public TransactionalTransport MainTransport { get; set; }

        public IBuilder Builder { get; set; }

        public static Func<IReceiveMessages> MessageReceiverFactory { get; set; }

        public void Start()
        {

            if (!Configure.Instance.IsTimeoutManagerEnabled())
                return;

            var messageReceiver = MessageReceiverFactory != null ? MessageReceiverFactory() : new MsmqMessageReceiver();

            inputTransport = new TransactionalTransport
            {
                MessageReceiver = messageReceiver,
                IsTransactional = !ConfigureVolatileQueues.IsVolatileQueues,
                NumberOfWorkerThreads = MainTransport.NumberOfWorkerThreads == 0 ? 1 : MainTransport.NumberOfWorkerThreads,
                MaxRetries = MainTransport.MaxRetries,
                FailureManager = Builder.Build(MainTransport.FailureManager.GetType())as IManageMessageFailures
            };

            inputTransport.TransportMessageReceived += OnTransportMessageReceived;

            inputTransport.Start(ConfigureTimeoutManager.TimeoutManagerAddress);
        }

        public void Stop()
        {
            if (inputTransport != null)
                inputTransport.Dispose();
        }

        void OnTransportMessageReceived(object sender, TransportMessageReceivedEventArgs e)
        {
            //dispatch request will arrive at the same input so we need to make sure to call the correct handler
            if (e.Message.Headers.ContainsKey(TimeoutDispatcher.TimeoutIdToDispatchHeader))
                Builder.Build<TimeoutDispatchHandler>().Handle(e.Message);
            else
                Builder.Build<TimeoutTransportMessageHandler>().Handle(e.Message);
        }

        ITransport inputTransport;
    }
}