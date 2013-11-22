namespace NServiceBus.Core.Tests.Pipeline
{
    using Unicast;

    class TransportOperation
    {
        public SendOptions SendOptions { get; set; }
        public TransportMessage PhysicalMessage { get; set; }
    }
}