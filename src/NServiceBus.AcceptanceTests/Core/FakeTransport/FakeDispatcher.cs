namespace NServiceBus.AcceptanceTests.Core.FakeTransport
{
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Transport;

    class FakeDispatcher : IDispatchMessages
    {
        public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, ContextBag context, CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }
    }
}