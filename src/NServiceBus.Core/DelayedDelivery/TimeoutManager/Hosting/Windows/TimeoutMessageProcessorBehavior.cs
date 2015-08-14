namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.ConsistencyGuarantees;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Extensibility;
    using NServiceBus.Pipeline;
    using NServiceBus.Timeout;
    using NServiceBus.Timeout.Core;
    using NServiceBus.Transports;

    class TimeoutMessageProcessorBehavior : SatelliteBehavior
    {
        readonly DefaultTimeoutManager defaultTimeoutManager;
        readonly IDispatchMessages dispatchMessages;

        public TimeoutMessageProcessorBehavior(IDispatchMessages dispatcher, DefaultTimeoutManager timeoutManager)
        {
            dispatchMessages = dispatcher;
            defaultTimeoutManager = timeoutManager;
        }

        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public string InputAddress { get; set; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public string EndpointName { get; set; }

        public override void Terminate(PhysicalMessageProcessingStageBehavior.Context context)
        {
            var message = context.GetPhysicalMessage();         
            var options = new TimeoutPersistenceOptions(context);
            //dispatch request will arrive at the same input so we need to make sure to call the correct handler
            if (message.Headers.ContainsKey(TimeoutIdToDispatchHeader))
            {
                HandleBackwardsCompatibility(message, options);
            }
            else
            {
                HandleInternal(message, options);
            }
        }

        void HandleBackwardsCompatibility(TransportMessage message, TimeoutPersistenceOptions options)
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

            defaultTimeoutManager.RemoveTimeout(timeoutId, options);
            dispatchMessages.Dispatch(new OutgoingMessage(message.Id, message.Headers, message.Body), new DispatchOptions(destination, new AtomicWithReceiveOperation(), new List<DeliveryConstraint>(), new ContextBag()));
        }

        void HandleInternal(TransportMessage message, TimeoutPersistenceOptions options)
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

                defaultTimeoutManager.RemoveTimeoutBy(sagaId, options);
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

                defaultTimeoutManager.PushTimeout(data, options);
            }
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
