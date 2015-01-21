namespace NServiceBus.Timeout.Hosting.Windows
{
    using System;
    using Core;
    using Faults;
    using ObjectBuilder;
    using Unicast;
    using Unicast.Queuing;
    using Unicast.Queuing.Msmq;
    using Unicast.Transport;
    using Unicast.Transport.Transactional;

    public class TimeoutMessageProcessor : IWantToRunWhenTheBusStarts, IDisposable 
    {
        const string TimeoutDestinationHeader = "NServiceBus.Timeout.Destination";
        const string TimeoutIdToDispatchHeader = "NServiceBus.Timeout.TimeoutIdToDispatch";

        ITransport inputTransport;

        public ISendMessages MessageSender { get; set; }

        public TransactionalTransport MainTransport { get; set; }

        public IBuilder Builder { get; set; }

        public IManageTimeouts TimeoutManager { get; set; }

        public static Func<IReceiveMessages> MessageReceiverFactory { get; set; }

        public void Run()
        {
            if (!Configure.Instance.IsTimeoutManagerEnabled())
            {
                return;
            }

            var messageReceiver = MessageReceiverFactory != null ? MessageReceiverFactory() : new MsmqMessageReceiver() { ErrorQueue = Configure.Instance.GetConfiguredErrorQueue() };

            inputTransport = new TransactionalTransport
            {
                MessageReceiver = messageReceiver,
                IsTransactional = true,
                NumberOfWorkerThreads = MainTransport.NumberOfWorkerThreads == 0 ? 1 : MainTransport.NumberOfWorkerThreads,
                MaxRetries = MainTransport.MaxRetries,
                FailureManager = new ManageMessageFailuresWithoutSlr(MainTransport.FailureManager),
            };

            inputTransport.TransportMessageReceived += OnTransportMessageReceived;

            inputTransport.Start(ConfigureTimeoutManager.TimeoutManagerAddress);
        }

        public void Dispose()
        {
            if (inputTransport != null)
            {
                inputTransport.Dispose();
            }
        }

        void OnTransportMessageReceived(object sender, TransportMessageReceivedEventArgs e)
        {
            //dispatch request will arrive at the same input so we need to make sure to call the correct handler
            if (e.Message.Headers.ContainsKey(TimeoutIdToDispatchHeader))
                HandleBackwardsCompatibility(e.Message);
            else
                Handle(e.Message);
        }

        void HandleBackwardsCompatibility(TransportMessage message)
        {
            var timeoutId = message.Headers[TimeoutIdToDispatchHeader];

            var destination = Address.Parse(message.Headers[TimeoutDestinationHeader]);

            //clear headers 
            message.Headers.Remove(TimeoutIdToDispatchHeader);
            message.Headers.Remove(TimeoutDestinationHeader);

            if (message.Headers.ContainsKey(Headers.RouteExpiredTimeoutTo))
            {
                destination = Address.Parse(message.Headers[Headers.RouteExpiredTimeoutTo]);
            }

            TimeoutManager.RemoveTimeout(timeoutId);
            MessageSender.Send(message, destination);
        }

        void Handle(TransportMessage message)
        {
            var sagaId = Guid.Empty;

            if (message.Headers.ContainsKey(Headers.SagaId))
            {
                sagaId = Guid.Parse(message.Headers[Headers.SagaId]);
            }

            if (message.Headers.ContainsKey(Headers.ClearTimeouts))
            {
                if (sagaId == Guid.Empty)
                    throw new InvalidOperationException("Invalid saga id specified, clear timeouts is only supported for saga instances");

                TimeoutManager.RemoveTimeoutBy(sagaId);
            }
            else
            {
                if (!message.Headers.ContainsKey(Headers.Expire))
                    throw new InvalidOperationException("Non timeout message arrived at the timeout manager, id:" + message.Id);

                var destination = message.ReplyToAddress;

                if (message.Headers.ContainsKey(Headers.RouteExpiredTimeoutTo))
                {
                    destination = Address.Parse(message.Headers[Headers.RouteExpiredTimeoutTo]);
                }
                
                var data = new TimeoutData
                {
                    Destination = destination,
                    SagaId = sagaId,
                    State = message.Body,
                    Time = message.Headers[Headers.Expire].ToUtcDateTime(),
                    CorrelationId = message.CorrelationId,
                    Headers = message.Headers,
                    OwningTimeoutManager = Configure.EndpointName
                };

                //add a temp header so that we can make sure to restore the ReplyToAddress
                if (message.ReplyToAddress != null)
                {
                    data.Headers[TimeoutData.OriginalReplyToAddress] = message.ReplyToAddress.ToString();
                }

                TimeoutManager.PushTimeout(data);
            }
        }
    }
}