namespace NServiceBus.Timeout.Core
{
    using System;
    using Unicast.Transport;

    public class TimeoutTransportMessageHandler
    {
        public IPersistTimeouts Persister { get; set; }
        public IManageTimeouts Manager { get; set; }

        public void Handle(TransportMessage message)
        {
            if(!message.Headers.ContainsKey(NServiceBus.Headers.Expire))
                throw new InvalidOperationException("Non timeout message arrived at the timeout manager, id:" + message.Id);

            var sagaId = Guid.Empty;

            if (message.Headers.ContainsKey(NServiceBus.Headers.SagaId))
                sagaId = Guid.Parse(message.Headers[NServiceBus.Headers.SagaId]);

                
            if (message.Headers.ContainsKey(Headers.ClearTimeout))
            {
                Manager.ClearTimeout(sagaId);
                Persister.Remove(sagaId);
            }
            else
            {
                var data = new TimeoutData
                               {
                                   Destination = message.ReplyToAddress,
                                   SagaId = sagaId,
                                   State = message.Body,
                                   Time = DateTime.Parse(message.Headers[NServiceBus.Headers.Expire])
                               };

                Manager.PushTimeout(data);
                Persister.Add(data);
            }
        }
    }
}
