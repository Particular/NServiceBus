namespace NServiceBus.AcceptanceTests.FakeTransport
{
    using NServiceBus.Transports;

    class FakeDispatcher : IDispatchMessages
    {
        public void Dispatch(OutgoingMessage message, DispatchOptions dispatchOptions)
        {

        }
    }
}