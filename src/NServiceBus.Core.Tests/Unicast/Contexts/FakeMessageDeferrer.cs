namespace NServiceBus.Unicast.Tests.Contexts
{
    using Transports;

    class FakeMessageDeferrer:IDeferMessages
    {
        public void Defer(TransportMessage message, SendOptions sendOptions)
        {
            

        }

        public void ClearDeferredMessages(string headerKey, string headerValue)
        {
            
        }
    }
}