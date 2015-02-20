namespace NServiceBus.Timeout.Hosting.Windows
{
    using System;
    using Core;
    using Satellites;
    using Transports;
    using Unicast;
    using Unicast.Transport;

    class TimeoutMessageProcessor : IAdvancedSatellite
    {
        public TimeoutMessageProcessor()
        {
            Disabled = true;
        }

        public ISendMessages MessageSender { get; set; }

        public Address InputAddress { get; set; }

        public DefaultTimeoutManager TimeoutManager { get; set; }

        public Configure Configure { get; set; }

        public bool Disabled { get; set; }
        public string EndpointName{ get; set; }

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
            };
        }

        public bool Handle(TransportMessage message)
        {
            //dispatch request will arrive at the same input so we need to make sure to call the correct handler
            if (message.Headers.ContainsKey(TimeoutIdToDispatchHeader))
            {
                HandleBackwardsCompatibility(message);
            }
            else
            {
                HandleInternal(message);
            }

            return true;
        }

        void HandleBackwardsCompatibility(TransportMessage message)
        {
            var timeoutId = message.Headers[TimeoutIdToDispatchHeader];

            var destination = Address.Parse(message.Headers[TimeoutDestinationHeader]);

            //clear headers 
            message.Headers.Remove(TimeoutIdToDispatchHeader);
            message.Headers.Remove(TimeoutDestinationHeader);

            string routeExpiredTimeoutTo;
            if (message.Headers.TryGetValue(TimeoutManagerHeaders.RouteExpiredTimeoutTo, out routeExpiredTimeoutTo))
            {
                destination = Address.Parse(routeExpiredTimeoutTo);
            }

            TimeoutManager.RemoveTimeout(timeoutId);
            MessageSender.Send(message, new SendOptions(destination));
        }

        void HandleInternal(TransportMessage message)
        {
            var sagaId = Guid.Empty;

            string sagaIdString;
            if (message.Headers.TryGetValue(Headers.SagaId, out sagaIdString))
            {
                sagaId = Guid.Parse(sagaIdString);
            }

            if (message.Headers.ContainsKey(TimeoutManagerHeaders.ClearTimeouts))
            {
                if (sagaId == Guid.Empty)
                    throw new InvalidOperationException("Invalid saga id specified, clear timeouts is only supported for saga instances");

                TimeoutManager.RemoveTimeoutBy(sagaId);
            }
            else
            {
                string expire;
                if (!message.Headers.TryGetValue(TimeoutManagerHeaders.Expire, out expire))
                {
                    throw new InvalidOperationException("Non timeout message arrived at the timeout manager, id:" + message.Id);
                }

                var destination = message.ReplyToAddress;

                string routeExpiredTimeoutTo;
                if (message.Headers.TryGetValue(TimeoutManagerHeaders.RouteExpiredTimeoutTo, out routeExpiredTimeoutTo))
                {
                    destination = Address.Parse(routeExpiredTimeoutTo);
                }
                
                var data = new TimeoutData
                {
                    Destination = destination,
                    SagaId = sagaId,
                    State = message.Body,
                    Time = DateTimeExtensions.ToUtcDateTime(expire),
                    Headers = message.Headers,
                    OwningTimeoutManager = EndpointName
                };

                //add a temp header so that we can make sure to restore the ReplyToAddress
                if (message.ReplyToAddress != null)
                {
                    data.Headers[TimeoutData.OriginalReplyToAddress] = message.ReplyToAddress.ToString();
                }

                TimeoutManager.PushTimeout(data);
            }
        }

        const string TimeoutDestinationHeader = "NServiceBus.Timeout.Destination";
        const string TimeoutIdToDispatchHeader = "NServiceBus.Timeout.TimeoutIdToDispatch";
    }
}
