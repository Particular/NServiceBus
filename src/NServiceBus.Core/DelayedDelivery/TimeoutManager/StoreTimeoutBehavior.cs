namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
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

        protected override async Task Terminate(IIncomingPhysicalMessageContext context)
        {
            var message = context.Message;
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

                await persister.RemoveTimeoutBy(sagaId, context.Extensions).ConfigureAwait(false);
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

                    await dispatcher.Dispatch(new[] { new TransportOperation(outgoingMessage, sendOptions) }, context.Extensions).ConfigureAwait(false);
                    return;
                }

                await persister.Add(data, context.Extensions).ConfigureAwait(false);

                poller.NewTimeoutRegistered(data.Time);
            }
        }

        public override Task Warmup()
        {
            poller.Start();
            return base.Warmup();
        }

        public override async Task Cooldown()
        {
            await poller.Stop().ConfigureAwait(false);
            await base.Cooldown().ConfigureAwait(false);
        }

        ExpiredTimeoutsPoller poller;
        IDispatchMessages dispatcher;
        IPersistTimeouts persister;
        string owningTimeoutManager;

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