namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using DelayedDelivery.TimeoutManager;
    using Pipeline;
    using Routing;
    using Timeout.Core;
    using Transports;

    class StoreTimeoutBehavior : SatelliteBehavior
    {
        public StoreTimeoutBehavior(ExpiredTimeoutsPoller poller, IDispatchMessages dispatcher, IPersistTimeouts persister, string owningTimeoutManager)
        {
            this.poller = poller;
            this.dispatcher = dispatcher;
            this.persister = persister;
            this.owningTimeoutManager = owningTimeoutManager;
        }

        protected override async Task Terminate(PhysicalMessageProcessingContext context)
        {
            var message = context.Message;

            //dispatch request will arrive at the same input so we need to make sure to call the correct handler
            if (message.Headers.ContainsKey(TimeoutIdToDispatchHeader))
            {
                await HandleBackwardsCompatibility(message, context).ConfigureAwait(false);
            }
            else
            {
                await HandleInternal(message, context).ConfigureAwait(false);
            }
        }

        public override Task Warmup()
        {
            poller.Start();
            return base.Warmup();
        }

        public override async Task Cooldown()
        {
            await poller.Stop();
            await base.Cooldown();
        }

        async Task HandleBackwardsCompatibility(IncomingMessage message, PhysicalMessageProcessingContext context)
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

            var timeoutData = await persister.Remove(timeoutId, context);

            if (timeoutData == null)
            {
                return;
            }

            var outgoingMessages = new OutgoingMessage(message.MessageId, message.Headers, message.Body);
            var dispatchOptions = new DispatchOptions(new UnicastAddressTag(destination), DispatchConsistency.Default);
            await dispatcher.Dispatch(new[] { new TransportOperation(outgoingMessages, dispatchOptions) }, context).ConfigureAwait(false);
        }

        async Task HandleInternal(IncomingMessage message, PhysicalMessageProcessingContext context)
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

                await persister.RemoveTimeoutBy(sagaId, context).ConfigureAwait(false);
            }
            else
            {
                string expire;
                if (!message.Headers.TryGetValue(TimeoutManagerHeaders.Expire, out expire))
                {
                    throw new InvalidOperationException("Non timeout message arrived at the timeout manager, id:" + message.MessageId);
                }

                var destination = message.GetReplyToAddress();

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
                    var sendOptions = new DispatchOptions(new UnicastAddressTag(data.Destination), DispatchConsistency.Default);
                    var outgoingMessage = new OutgoingMessage(message.MessageId, data.Headers, data.State);

                    await dispatcher.Dispatch(new[] { new TransportOperation(outgoingMessage, sendOptions) }, context).ConfigureAwait(false);
                    return;
                }

                await persister.Add(data, context).ConfigureAwait(false);

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