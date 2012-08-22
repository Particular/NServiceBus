namespace NServiceBus.Timeout.Core.Dispatch
{
    using Unicast.Queuing;
    using Unicast.Transport;

    public class TimeoutDispatchHandler
    {
        public IPersistTimeouts Persister { get; set; }
        public ISendMessages MessageSender { get; set; }

        public void Handle(TransportMessage message)
        {
            var timeoutId = message.Headers[TimeoutDispatcher.TimeoutIdToDispatchHeader];

            var destination = Address.Parse(message.Headers[TimeoutDispatcher.TimeoutDestinationHeader]);

            //clear headers 
            message.Headers.Remove(TimeoutDispatcher.TimeoutIdToDispatchHeader);
            message.Headers.Remove(TimeoutDispatcher.TimeoutDestinationHeader);

            if(message.Headers.ContainsKey(Headers.RouteExpiredTimeoutTo))
                destination = Address.Parse(message.Headers[Headers.RouteExpiredTimeoutTo]);

            MessageSender.Send(message,destination);
            
            Persister.Remove(timeoutId);
        }


    }
}