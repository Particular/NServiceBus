namespace NServiceBus.Core.Tests.Pipeline
{
    using Unicast;

    class TransportOperation
    {
        public TransportOperation(SendOptions sendOptions, TransportMessage physicalMessage)
        {
            SendOptions = sendOptions;
            PhysicalMessage = physicalMessage;
        }

        public SendOptions SendOptions { get; private set; }
        public TransportMessage PhysicalMessage { get;private set; }
    }
}