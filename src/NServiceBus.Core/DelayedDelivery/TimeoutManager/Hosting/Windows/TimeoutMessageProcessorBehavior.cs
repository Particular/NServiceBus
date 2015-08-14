namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.ConsistencyGuarantees;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Extensibility;
    using NServiceBus.Pipeline;
    using NServiceBus.Timeout;
    using NServiceBus.Timeout.Core;
    using NServiceBus.Transports;

    class TimeoutMessageProcessorBehavior : SatelliteBehavior
    {
        public IDispatchMessages MessageSender { get; set; }

        public string InputAddress { get; set; }

        public DefaultTimeoutManager TimeoutManager { get; set; }

        public Configure Configure { get; set; }

        public string EndpointName { get; set; }

        public override async Task Terminate(PhysicalMessageProcessingStageBehavior.Context context)
        {
            var message = context.GetPhysicalMessage();         
            //dispatch request will arrive at the same input so we need to make sure to call the correct handler
            if (message.Headers.ContainsKey(TimeoutIdToDispatchHeader))
            {
                await HandleBackwardsCompatibility(message);
            }
            else
            {
                await HandleInternal(message);
            }
        }

        Task HandleBackwardsCompatibility(TransportMessage message)
        {
            var timeoutId = message.Headers[TimeoutIdToDispatchHeader];

            var destination = message.Headers[TimeoutDestinationHeader];

            //clear headers 
            message.Headers.Remove(TimeoutIdToDispatchHeader);
            message.Headers.Remove(TimeoutDestinationHeader);

            string routeExpiredTimeoutTo;
            if (message.Headers.TryGetValue(TimeoutManagerHeaders.RouteExpiredTimeoutTo, out routeExpiredTimeoutTo))
            {
                destination = routeExpiredTimeoutTo;
            }

            TimeoutManager.RemoveTimeout(timeoutId);
            return MessageSender.Dispatch(new OutgoingMessage(message.Id,message.Headers, message.Body), new DispatchOptions(destination,new AtomicWithReceiveOperation(), new List<DeliveryConstraint>(), new ContextBag()));
        }

        Task HandleInternal(TransportMessage message)
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
                    destination = routeExpiredTimeoutTo;
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



                TimeoutManager.PushTimeout(data);
            }

            return Task.FromResult(true);
        }

        const string TimeoutDestinationHeader = "NServiceBus.Timeout.Destination";

        const string TimeoutIdToDispatchHeader = "NServiceBus.Timeout.TimeoutIdToDispatch";

        public class Registration : RegisterStep
        {
            public Registration()
                : base("TimeoutMessageProcessor", typeof(TimeoutMessageProcessorBehavior), "Processes timeout messages")
            {
                InsertBeforeIfExists("FirstLevelRetries");
                InsertBeforeIfExists("ReceivePerformanceDiagnosticsBehavior");
            }
        }
    }
}
