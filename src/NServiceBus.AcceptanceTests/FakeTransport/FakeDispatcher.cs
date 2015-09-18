namespace NServiceBus.AcceptanceTests.FakeTransport
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Transports;

    class FakeDispatcher : IDispatchMessages
    {
        public Task Dispatch(IEnumerable<TransportOperation> outgoingMessages)
        {
            return Task.FromResult(0);
        }
    }
}