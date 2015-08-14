namespace NServiceBus.Timeout.Core
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.ConsistencyGuarantees;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Extensibility;
    using NServiceBus.Transports;

    class DefaultTimeoutManager
    {
        readonly IPersistTimeouts persistTimeouts;
        readonly IDispatchMessages dispatchMessages;

        public DefaultTimeoutManager(IPersistTimeouts timeoutsPersister, IDispatchMessages messageDispatcher)
        {
            dispatchMessages = messageDispatcher;
            persistTimeouts = timeoutsPersister;
        }

        public Action<TimeoutData> TimeoutPushed;

        public void PushTimeout(TimeoutData timeout, TimeoutPersistenceOptions options)
        {
            if (timeout.Time.AddSeconds(-1) <= DateTime.UtcNow)
            {
                var sendOptions = new DispatchOptions(timeout.Destination,new AtomicWithReceiveOperation(), new List<DeliveryConstraint>(), new ContextBag());
                var message = new OutgoingMessage(timeout.Headers[Headers.MessageId],timeout.Headers, timeout.State);

                dispatchMessages.Dispatch(message, sendOptions);
                return;
            }


            persistTimeouts.Add(timeout, options);

            if (TimeoutPushed != null)
            {
                TimeoutPushed(timeout);
            }
        }

        public void RemoveTimeout(string timeoutId, TimeoutPersistenceOptions options)
        {
            TimeoutData timeoutData;

            persistTimeouts.TryRemove(timeoutId, options, out timeoutData);
        }

        public void RemoveTimeoutBy(Guid sagaId, TimeoutPersistenceOptions options)
        {
            persistTimeouts.RemoveTimeoutBy(sagaId, options);
        }
    }
}
