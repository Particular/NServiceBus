using System.Threading;

namespace NServiceBus.AcceptanceTests.Core.FakeTransport
{
    using System.Threading.Tasks;
    using Extensibility;
    using Transport;

    class FakeDispatcher : IDispatchMessages
    {
        public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }
    }
}