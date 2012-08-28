namespace NServiceBus.Timeout.Core
{
    using System;
    using System.Collections.Generic;
    using Unicast.Queuing;
    using Unicast.Transport;

    public class DefaultTimeoutManager : IManageTimeouts
    {
        const string OriginalReplyToAddress = "NServiceBus.Timeout.ReplyToAddress";


        public IPersistTimeouts TimeoutsPersister { get; set; }

        public ISendMessages MessageSender { get; set; }

        public event EventHandler<TimeoutData> TimeoutPushed;

        public void PushTimeout(TimeoutData timeout)
        {
            if (timeout.Time <= DateTime.UtcNow)
            {
                MessageSender.Send(MapToTransportMessage(timeout), timeout.Destination);
                return;
            }

            TimeoutsPersister.Add(timeout);

            if (TimeoutPushed != null)
                TimeoutPushed.BeginInvoke(this, timeout, ar => {}, null);
        }

        public void RemoveTimeout(string timeoutId)
        {
            TimeoutsPersister.TryRemove(timeoutId);
        }

        public void RemoveTimeoutBy(Guid sagaId)
        {
            TimeoutsPersister.RemoveTimeoutBy(sagaId);
        }

        static TransportMessage MapToTransportMessage(TimeoutData timeoutData)
        {
            var replyToAddress = Address.Local;
            if (timeoutData.Headers != null && timeoutData.Headers.ContainsKey(OriginalReplyToAddress))
            {
                replyToAddress = Address.Parse(timeoutData.Headers[OriginalReplyToAddress]);
                timeoutData.Headers.Remove(OriginalReplyToAddress);
            }

            var transportMessage = new TransportMessage
            {
                ReplyToAddress = replyToAddress,
                Headers = new Dictionary<string, string>(),
                Recoverable = true,
                MessageIntent = MessageIntentEnum.Send,
                CorrelationId = timeoutData.CorrelationId,
                Body = timeoutData.State
            };

            if (timeoutData.Headers != null)
            {
                transportMessage.Headers = timeoutData.Headers;
            }
            else if (timeoutData.SagaId != Guid.Empty)
            {
                transportMessage.Headers[Headers.SagaId] = timeoutData.SagaId.ToString();
            }

            return transportMessage;
        }
    }
}
