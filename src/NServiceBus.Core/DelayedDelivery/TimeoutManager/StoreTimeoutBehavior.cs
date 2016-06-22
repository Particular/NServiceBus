namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Routing;
    using Timeout.Core;
    using Transports;

    class StoreTimeoutBehavior
    {
        public StoreTimeoutBehavior(ExpiredTimeoutsPoller poller, IDispatchMessages dispatcher, IPersistTimeouts persister, string owningTimeoutManager)
        {
            this.poller = poller;
            this.dispatcher = dispatcher;
            this.persister = persister;
            this.owningTimeoutManager = owningTimeoutManager;
        }

        public async Task Invoke(PushContext context)
        {
            var sagaId = Guid.Empty;

            string sagaIdString;
            if (context.Headers.TryGetValue(Headers.SagaId, out sagaIdString))
            {
                sagaId = Guid.Parse(sagaIdString);
            }

            if (context.Headers.ContainsKey(TimeoutManagerHeaders.ClearTimeouts))
            {
                if (sagaId == Guid.Empty)
                {
                    throw new InvalidOperationException("Invalid saga id specified, clear timeouts is only supported for saga instances");
                }

                await persister.RemoveTimeoutBy(sagaId, context.Context).ConfigureAwait(false);
            }
            else
            {
                string expire;
                if (!context.Headers.TryGetValue(TimeoutManagerHeaders.Expire, out expire))
                {
                    throw new InvalidOperationException("Non timeout message arrived at the timeout manager, id:" + context.MessageId);
                }

                var destination = GetReplyToAddress(context);

                string routeExpiredTimeoutTo;
                if (context.Headers.TryGetValue(TimeoutManagerHeaders.RouteExpiredTimeoutTo, out routeExpiredTimeoutTo))
                {
                    destination = routeExpiredTimeoutTo;
                }
                var body = new byte[context.BodyStream.Length];
                context.BodyStream.Read(body, 0, body.Length);
                var data = new TimeoutData
                {
                    Destination = destination,
                    SagaId = sagaId,
                    State = body,
                    Time = DateTimeExtensions.ToUtcDateTime(expire),
                    Headers = context.Headers,
                    OwningTimeoutManager = owningTimeoutManager
                };

                if (data.Time.AddSeconds(-1) <= DateTime.UtcNow)
                {
                    var outgoingMessage = new OutgoingMessage(context.MessageId, data.Headers, data.State);
                    var transportOperation = new TransportOperation(outgoingMessage, new UnicastAddressTag(data.Destination));
                    await dispatcher.Dispatch(new TransportOperations(transportOperation), context.Context).ConfigureAwait(false);
                    return;
                }

                await persister.Add(data, context.Context).ConfigureAwait(false);

                poller.NewTimeoutRegistered(data.Time);
            }
        }

        static string GetReplyToAddress(PushContext context)
        {
            string replyToAddress;
            return context.Headers.TryGetValue(Headers.ReplyToAddress, out replyToAddress) ? replyToAddress : null;
        }

        IDispatchMessages dispatcher;
        string owningTimeoutManager;
        IPersistTimeouts persister;

        ExpiredTimeoutsPoller poller;
    }
}