using NServiceBus.Unicast.Transport;

namespace NServiceBus.Satellites.Tests
{
    public class FakeTransportBuilder : ISatelliteTransportBuilder
    {
        public ITransport TransportToReturn { get; set; }

        public ITransport Build(int numberOfWorkerThreads, int maxRetries, bool isTransactional)
        {
            return TransportToReturn ?? new FakeTransport();
        }
    }
}