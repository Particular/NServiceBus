namespace NServiceBus.Timeout.Core
{
    using System;
    using NServiceBus;
    using Unicast.Transport;

    public class TimeoutMessageHandler
    {
        public IPersistTimeouts Persister { get; set; }
        public IManageTimeouts Manager { get; set; }
        public IBus Bus { get; set; }

        public void Handle(TransportMessage message)
        {
            if(!message.Headers.ContainsKey(Headers.IsTimeoutMessage))
                throw new InvalidOperationException("Non timeout message arrived at the timeout manager, id:" + message.Id);

            var sagaId = Guid.Empty;

            if (message.Headers.ContainsKey(Headers.SagaId))
                sagaId = Guid.Parse(message.Headers[Headers.SagaId]);

                
            if (message.Headers.ContainsKey(Headers.ClearTimeout))
            {
                Manager.ClearTimeout(sagaId);
                Persister.Remove(sagaId);
            }
            else
            {
                var data = new TimeoutData
                               {
                                   Destination = Bus.CurrentMessageContext.ReplyToAddress,
                                   SagaId = sagaId,
                                   State = message.Body,
                                   Time = DateTime.Parse(message.Headers[Headers.Expire])
                               };

                Manager.PushTimeout(data);
                Persister.Add(data);
            }
        }
    }
}
