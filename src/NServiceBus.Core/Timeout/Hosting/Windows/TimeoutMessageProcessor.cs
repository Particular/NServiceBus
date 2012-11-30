namespace NServiceBus.Timeout.Hosting.Windows
{
    using System;
    using Core;
    using Satellites;
    using Unicast.Queuing;


    public class TimeoutMessageProcessor : ISatellite
    {
        private bool enabled = true;

        const string TimeoutDestinationHeader = "NServiceBus.Timeout.Destination";
        const string TimeoutIdToDispatchHeader = "NServiceBus.Timeout.TimeoutIdToDispatch";

        static readonly Address TimeoutManagerAddress;

        public ISendMessages MessageSender { get; set; }

        public IManageTimeouts TimeoutManager { get; set; }

        static TimeoutMessageProcessor()
        {
            TimeoutManagerAddress = Address.Parse(Configure.EndpointName).SubScope("Timeouts");            
        }

        public Address InputAddress { get { return TimeoutManagerAddress; } }

        public bool Disabled { get { return !enabled; } }

        public void Disable()
        {
            enabled = false;
        }

        public void Start()
        {

        }

        public void Stop()
        {
            
        }

        public void Handle(TransportMessage message)
        {
            //dispatch request will arrive at the same input so we need to make sure to call the correct handler
            if (message.Headers.ContainsKey(TimeoutIdToDispatchHeader))
                HandleBackwardsCompatibility(message);
            else
                HandleInternal(message);
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

        void HandleInternal(TransportMessage message)
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
                    Time = DateTimeExtensions.ToUtcDateTime(message.Headers[Headers.Expire]),
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
