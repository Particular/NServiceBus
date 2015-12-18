namespace NServiceBus.AcceptanceTests.FakeTransport
{
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Transports;

    class FakeDispatcher : IDispatchMessages
    {
        public Task Dispatch(TransportOperations outgoingMessages, ContextBag context)
        {
            return Task.FromResult(0);
        }
    }
}