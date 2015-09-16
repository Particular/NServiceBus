namespace NServiceBus.AcceptanceTests.FakeTransport
{
    using System.Threading.Tasks;
    using NServiceBus.Transports;

    class FakeDispatcher : IDispatchMessages
    {
        public Task Dispatch(OutgoingMessage message, DispatchOptions dispatchOptions)
        {
            return Task.FromResult(0);
        }
    }
}