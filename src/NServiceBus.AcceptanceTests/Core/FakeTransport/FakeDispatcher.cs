namespace NServiceBus.AcceptanceTests.Core.FakeTransport
{
    using System.Threading.Tasks;
    using Transport;

    class FakeDispatcher : IMessageDispatcher
    {
        public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction)
        {
            return Task.CompletedTask;
        }
    }
}