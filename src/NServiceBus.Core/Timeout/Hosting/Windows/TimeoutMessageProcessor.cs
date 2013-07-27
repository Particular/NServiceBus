namespace NServiceBus.Timeout.Hosting.Windows
{
    using System;
    using Core;
    using Features;
    using Satellites;
    using Transports;
    using Transports.Msmq;
    using Unicast.Transport;


    public class TimeoutMessageProcessor : IAdvancedSatellite
    {
        public ISendMessages MessageSender { get; set; }

        public IManageTimeouts TimeoutManager { get; set; }

        public Address InputAddress { get { return Features.TimeoutManager.InputAddress; } }

        public bool Disabled
        {
            get { return !Feature.IsEnabled<TimeoutManager>(); }
        }

        public void Start()
        {

        }

        public void Stop()
        {
            
        }

        public Action<TransportReceiver> GetReceiverCustomization()
        {
            return receiver =>
            {
                //TODO: The line below needs to change when we refactor the slr to be:
                // transport.DisableSLR() or similar
                receiver.FailureManager = new ManageMessageFailuresWithoutSlr(receiver.FailureManager);
            };
        }

        public bool Handle(TransportMessage message)
        {
            //dispatch request will arrive at the same input so we need to make sure to call the correct handler
            if (message.Headers.ContainsKey(TimeoutIdToDispatchHeader))
                HandleBackwardsCompatibility(message);
            else
                HandleInternal(message);

            return true;
        }

        void HandleBackwardsCompatibility(TransportMessage message)
        {
            var timeoutId = message.Headers[TimeoutIdToDispatchHeader];

            var destination = Address.Parse(message.Headers[TimeoutDestinationHeader]);

            //clear headers 
            message.Headers.Remove(TimeoutIdToDispatchHeader);
            message.Headers.Remove(TimeoutDestinationHeader);

            if (message.Headers.ContainsKey(TimeoutManagerHeaders.RouteExpiredTimeoutTo))
            {
                destination = Address.Parse(message.Headers[TimeoutManagerHeaders.RouteExpiredTimeoutTo]);
            }

            TimeoutManager.RemoveTimeout(timeoutId);
            MessageSender.Send(message, destination);
        }

        void HandleInternal(TransportMessage message)
        {
            var sagaId = Guid.Empty;

            if (message.Headers.ContainsKey(Headers.SagaId))
            {
                sagaId = Guid.Parse(message.Headers[Headers.SagaId]);
            }

            if (message.Headers.ContainsKey(TimeoutManagerHeaders.ClearTimeouts))
            {
                if (sagaId == Guid.Empty)
                    throw new InvalidOperationException("Invalid saga id specified, clear timeouts is only supported for saga instances");

                TimeoutManager.RemoveTimeoutBy(sagaId);
            }
            else
            {
                if (!message.Headers.ContainsKey(TimeoutManagerHeaders.Expire))
                    throw new InvalidOperationException("Non timeout message arrived at the timeout manager, id:" + message.Id);

                var destination = message.ReplyToAddress;

                if (message.Headers.ContainsKey(TimeoutManagerHeaders.RouteExpiredTimeoutTo))
                {
                    destination = Address.Parse(message.Headers[TimeoutManagerHeaders.RouteExpiredTimeoutTo]);
                }
                
                var data = new TimeoutData
                {
                    Destination = destination,
                    SagaId = sagaId,
                    State = message.Body,
                    Time = DateTimeExtensions.ToUtcDateTime(message.Headers[TimeoutManagerHeaders.Expire]),
                    CorrelationId = GetCorrelationIdToStore(message),
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

        [ObsoleteEx(RemoveInVersion ="5.0")]
        string GetCorrelationIdToStore(TransportMessage message)
        {
            var correlationIdToStore = message.CorrelationId;
            
            if (MessageSender is MsmqMessageSender)
            {
                Guid correlationId;
                
                if (Guid.TryParse(message.CorrelationId, out correlationId))
                {
                    correlationIdToStore = message.CorrelationId + "\\0";//msmq required the id's to be in the {guid}\{incrementing number} format so we need to fake a \0 at the end to make it compatible                
                }
            }

            return correlationIdToStore;
        }

        const string TimeoutDestinationHeader = "NServiceBus.Timeout.Destination";
        const string TimeoutIdToDispatchHeader = "NServiceBus.Timeout.TimeoutIdToDispatch";
    }
}
