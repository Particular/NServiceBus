using NServiceBus.Unicast.Queuing;

namespace NServiceBus.Timeout.Hosting.Windows
{
    using System;
    using Core;
    using Core.Dispatch;
    using Faults;
    using ObjectBuilder;
    using Unicast;
    using Unicast.Queuing.Msmq;
    using Unicast.Transport;
    using Unicast.Transport.Transactional;

    // HACK: Intentionally using obsoleted IWantToRunWhenTheBusStarts interface to ensure backwards compatability.
    // This can be changed to use new interface once old interface is removed. 
    public class TimeoutMessageProcessor : IWantToRunWhenTheBusStarts, IDisposable 
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
            //dispatch request will arrive at the same input so we need to make sure to call the correct handler
            if (e.Message.Headers.ContainsKey(TimeoutDispatcher.TimeoutIdToDispatchHeader))
                Builder.Build<TimeoutDispatchHandler>().Handle(e.Message);
            else
                Builder.Build<TimeoutTransportMessageHandler>().Handle(e.Message);
        }


     
        public void Dispose()
        {
            if (inputTransport != null)
                inputTransport.Dispose();
        }


        ITransport inputTransport;
    }
}