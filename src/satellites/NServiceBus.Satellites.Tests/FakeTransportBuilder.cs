using NServiceBus.Unicast.Transport;

namespace NServiceBus.Satellites.Tests
{
    public class FakeTransportBuilder : ISatelliteTransportBuilder
    {
        public ITransport TransportToReturn { get; set; }

        public ITransport Build()
        {
            return TransportToReturn ?? new FakeTransport();
        }
    }
}