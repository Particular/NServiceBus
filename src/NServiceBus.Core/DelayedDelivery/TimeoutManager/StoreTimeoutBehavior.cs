namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Routing;
    using Timeout.Core;
    using Transport;

    class StoreTimeoutBehavior
    {
        public StoreTimeoutBehavior(ExpiredTimeoutsPoller poller, IDispatchMessages dispatcher, IPersistTimeouts persister, string owningTimeoutManager)
        {
            this.poller = poller;
            this.dispatcher = dispatcher;
            this.persister = persister;
            this.owningTimeoutManager = owningTimeoutManager;
        }

        public async Task Invoke(MessageContext context)
        {
            var sagaId = Guid.Empty;

            if (context.Headers.TryGetValue(Headers.SagaId, out var sagaIdString))
            {
                sagaId = Guid.Parse(sagaIdString);
            }

            if (context.Headers.ContainsKey(TimeoutManagerHeaders.ClearTimeouts))
            {
                if (sagaId == Guid.Empty)
                {
                    throw new InvalidOperationException("Invalid saga id specified, clear timeouts is only supported for saga instances");
                }

                await persister.RemoveTimeoutBy(sagaId, context.Extensions).ConfigureAwait(false);
            }
            else
            {
                if (!context.Headers.TryGetValue(TimeoutManagerHeaders.Expire, out var expire))
                {
                    throw new InvalidOperationException("Non timeout message arrived at the timeout manager, id:" + context.MessageId);
                }

                var destination = GetReplyToAddress(context);

                if (context.Headers.TryGetValue(TimeoutManagerHeaders.RouteExpiredTimeoutTo, out var routeExpiredTimeoutTo))
                {
                    destination = routeExpiredTimeoutTo;
                }

                var data = new TimeoutData
                {
                    Destination = destination,
                    SagaId = sagaId,
                    State = context.Body,
                    Time = DateTimeOffsetHelper.ToDateTimeOffset(expire),
                    Headers = context.Headers,
                    OwningTimeoutManager = owningTimeoutManager
                };

                if (data.Time.AddSeconds(-1) <= DateTimeOffset.UtcNow)
                {
                    var outgoingMessage = new OutgoingMessage(context.MessageId, data.Headers, data.State);
                    var transportOperation = new TransportOperation(outgoingMessage, new UnicastAddressTag(data.Destination));
                    await dispatcher.Dispatch(new TransportOperations(transportOperation), context.TransportTransaction, context.Extensions).ConfigureAwait(false);
                    return;
                }

                await persister.Add(data, context.Extensions).ConfigureAwait(false);

                poller.NewTimeoutRegistered(data.Time);
            }
        }

        static string GetReplyToAddress(MessageContext context)
        {
            return context.Headers.TryGetValue(Headers.ReplyToAddress, out var replyToAddress) ? replyToAddress : null;
        }

        IDispatchMessages dispatcher;
        string owningTimeoutManager;
        IPersistTimeouts persister;

        ExpiredTimeoutsPoller poller;
    }
}