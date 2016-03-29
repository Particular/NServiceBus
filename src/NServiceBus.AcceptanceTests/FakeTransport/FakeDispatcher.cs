namespace NServiceBus.AcceptanceTests.FakeTransport
{
    using System.Threading.Tasks;
    using Extensibility;
    using Transports;

    class FakeDispatcher : IDispatchMessages
    {
        public Task Dispatch(TransportOperations outgoingMessages, ContextBag context)
        {
            return Task.FromResult(0);
        }
    }
}