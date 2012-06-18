namespace NServiceBus.Timeout.Core.Dispatch
{
    using System;
    using Unicast.Queuing;
    using Unicast.Transport;

    public class TimeoutDispatchHandler
    {
        public IPersistTimeouts Persister { get; set; }
        public ISendMessages MessageSender { get; set; }

        public void Handle(TransportMessage message)
        {
            var timeoutId = message.Headers[TimeoutDispatcher.TimeoutIdToDispatchHeader];

            if(string.IsNullOrEmpty(timeoutId))
                throw new InvalidOperationException("Can't dispatch timeout, header " + TimeoutDispatcher.TimeoutIdToDispatchHeader + " is null or empty");

            var destination = Address.Parse(message.Headers[TimeoutDispatcher.TimeoutDestinationHeader]);

            //clear headers 
            message.Headers.Remove(TimeoutDispatcher.TimeoutIdToDispatchHeader);
            message.Headers.Remove(TimeoutDispatcher.TimeoutDestinationHeader);

            MessageSender.Send(message,destination);
            
            Persister.Remove(timeoutId);
        }


    }
}