namespace NServiceBus.Timeout.Core
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.ConsistencyGuarantees;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Transports;

    class DefaultTimeoutManager
    {
        public IPersistTimeouts TimeoutsPersister { get; set; }

        public IDispatchMessages MessageSender { get; set; }

        public Action<TimeoutData> TimeoutPushed;

        public void PushTimeout(TimeoutData timeout)
        {
            if (timeout.Time.AddSeconds(-1) <= DateTime.UtcNow)
            {
                var sendOptions = new DispatchOptions(timeout.Destination,new AtomicWithReceiveOperation(), new List<DeliveryConstraint>());
                var message = new OutgoingMessage(timeout.Headers[Headers.MessageId],timeout.Headers, timeout.State);

                MessageSender.Dispatch(message, sendOptions);
                return;
            }


            TimeoutsPersister.Add(timeout);

            if (TimeoutPushed != null)
            {
                TimeoutPushed(timeout);
            }
        }

        public void RemoveTimeout(string timeoutId)
        {
            TimeoutData timeoutData;

            TimeoutsPersister.TryRemove(timeoutId, out timeoutData);
        }

        public void RemoveTimeoutBy(Guid sagaId)
        {
            TimeoutsPersister.RemoveTimeoutBy(sagaId);
        }
    }
}
