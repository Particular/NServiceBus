namespace NServiceBus.Outbox
{
    using Unicast;

    public class TransportOperation
    {
        public TransportOperation(SendOptions sendOptions, TransportMessage message)
        {
            SendOptions = sendOptions;
            Message = message;
        }

        public SendOptions SendOptions { get; private set; }
        public TransportMessage Message { get;private set; }
    }
}