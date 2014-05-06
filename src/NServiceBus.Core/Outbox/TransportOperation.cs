namespace NServiceBus.Outbox
{
    using Unicast;

    public class TransportOperation
    {
        public TransportOperation(SendOptions sendOptions, TransportMessage message, string messageType)
        {
            SendOptions = sendOptions;
            Message = message;
            MessageType = messageType;
        }

        public SendOptions SendOptions { get; private set; }
        public TransportMessage Message { get;private set; }
        public string MessageType { get; private set; }
    }
}