namespace NServiceBus.Timeout.Core
{
    using System;
    using Unicast.Transport;

    public class TimeoutTransportMessageHandler
    {
        public const string OriginalReplyToAddress = "NServiceBus.Timeout.ReplyToAddress";
        public IPersistTimeouts Persister { get; set; }
        
        public IManageTimeouts Manager { get; set; }

        public void Handle(TransportMessage message)
        {
            var sagaId = Guid.Empty;

            if (message.Headers.ContainsKey(Headers.SagaId))
                sagaId = Guid.Parse(message.Headers[Headers.SagaId]);

                
            if (message.Headers.ContainsKey(Headers.ClearTimeouts))
            {
                if(sagaId == Guid.Empty)
                    throw new InvalidOperationException("Invalid saga id specified, clear timeouts is only supported for saga instances");

                Persister.ClearTimeoutsFor(sagaId);
                Manager.ClearTimeout(sagaId);
            }
            else
            {
                if (!message.Headers.ContainsKey(Headers.Expire))
                    throw new InvalidOperationException("Non timeout message arrived at the timeout manager, id:" + message.Id);
                
                var data = new TimeoutData
                               {
                                   Destination = message.ReplyToAddress,
                                   SagaId = sagaId,
                                   State = message.Body,
                                   Time = message.Headers[Headers.Expire].ToUtcDateTime(),
                                   CorrelationId = message.CorrelationId,
                                   Headers = message.Headers,
                                   OwningTimeoutManager = Configure.EndpointName
                               };

                //add a temp header so that we can make sure to restore the ReplyToAddress
                if(message.ReplyToAddress != null)
                    data.Headers[OriginalReplyToAddress] = message.ReplyToAddress.ToString();

                Persister.Add(data);
                Manager.PushTimeout(data);
            }
        }
    }
}
