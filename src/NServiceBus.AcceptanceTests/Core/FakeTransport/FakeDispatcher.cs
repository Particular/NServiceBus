namespace NServiceBus.AcceptanceTests.Core.FakeTransport
{
    using System.Threading.Tasks;
    using Extensibility;
    using Transport;

    class FakeDispatcher : IDispatchMessages
    {
        public Task Dispatch(TransportOperations outgoingMessages, ContextBag context)
        {
            return Task.FromResult(0);
        }
    }
}