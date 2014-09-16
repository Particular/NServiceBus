namespace NServiceBus.Timeout.Core
{
    using System;
    using Transports;

    class DefaultTimeoutManager
    {
        public IPersistTimeouts TimeoutsPersister { get; set; }
       
        public ISendMessages MessageSender { get; set; }

        public Configure Configure { get; set; }

        public Action<TimeoutData> TimeoutPushed;

        public void PushTimeout(TimeoutData timeout)
        {
            if (timeout.Time.AddSeconds(-1) <= DateTime.UtcNow)
            {
                MessageSender.Send(timeout.ToTransportMessage(), timeout.ToSendOptions(Configure.LocalAddress));
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
