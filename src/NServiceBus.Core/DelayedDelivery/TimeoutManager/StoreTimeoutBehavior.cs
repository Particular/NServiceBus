namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.ConsistencyGuarantees;
    using NServiceBus.DelayedDelivery.TimeoutManager;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Pipeline;
    using NServiceBus.Timeout.Core;
    using NServiceBus.Transports;

    class StoreTimeoutBehavior : SatelliteBehavior
    {
        public StoreTimeoutBehavior(ExpiredTimeoutsPoller poller, IDispatchMessages dispatcher, IPersistTimeouts persister, string owningTimeoutManager)
        {
            this.poller = poller;
            this.dispatcher = dispatcher;
            this.persister = persister;
            this.owningTimeoutManager = owningTimeoutManager;
        }

        public override void Terminate(PhysicalMessageProcessingStageBehavior.Context context)
        {
            var message = context.GetPhysicalMessage();

            //dispatch request will arrive at the same input so we need to make sure to call the correct handler
            if (message.Headers.ContainsKey(TimeoutIdToDispatchHeader))
            {
                HandleBackwardsCompatibility(message, context);
            }
            else
            {
                HandleInternal(message, context);
            }
        }

        public override Task Warmup()
        {
            poller.Start();
            return base.Warmup();
        }

        public override Task Cooldown()
        {
            poller.Stop();
            return base.Cooldown();
        }

        void HandleBackwardsCompatibility(TransportMessage message, PhysicalMessageProcessingStageBehavior.Context context)
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

            TimeoutData timeoutData;

            if (!persister.TryRemove(timeoutId, new TimeoutPersistenceOptions(context), out timeoutData))
            {
                return;
            }

            dispatcher.Dispatch(new OutgoingMessage(message.Id, message.Headers, message.Body), new DispatchOptions(destination, new AtomicWithReceiveOperation(), new List<DeliveryConstraint>(), context));
        }

        void HandleInternal(TransportMessage message, PhysicalMessageProcessingStageBehavior.Context context)
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

                persister.RemoveTimeoutBy(sagaId, new TimeoutPersistenceOptions(context));
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
                    OwningTimeoutManager = owningTimeoutManager
                };

                if (data.Time.AddSeconds(-1) <= DateTime.UtcNow)
                {
                    var sendOptions = new DispatchOptions(data.Destination, new AtomicWithReceiveOperation(), new List<DeliveryConstraint>(), context);
                    var outgoingMessage = new OutgoingMessage(data.Headers[Headers.MessageId], data.Headers, data.State);

                    dispatcher.Dispatch(outgoingMessage, sendOptions);
                    return;
                }


                persister.Add(data, new TimeoutPersistenceOptions(context));

                poller.NewTimeoutRegistered(data.Time);
            }
        }

        ExpiredTimeoutsPoller poller;
        IDispatchMessages dispatcher;
        IPersistTimeouts persister;
        string owningTimeoutManager;

        const string TimeoutDestinationHeader = "NServiceBus.Timeout.Destination";
        const string TimeoutIdToDispatchHeader = "NServiceBus.Timeout.TimeoutIdToDispatch";

        public class Registration : RegisterStep
        {
            public Registration()
                : base("TimeoutMessageProcessor", typeof(StoreTimeoutBehavior), "Processes timeout messages")
            {
                InsertBeforeIfExists("FirstLevelRetries");
                InsertBeforeIfExists("ReceivePerformanceDiagnosticsBehavior");
            }
        }
    }
}
