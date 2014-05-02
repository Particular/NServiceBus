namespace NServiceBus.Unicast.Tests.Contexts
{
    using System;
    using Transports;

    class FakeMessageDeferrer:IDeferMessages
    {
        public void Defer(TransportMessage message, DateTime processAt, Address address)
        {
            
        }

        public void Defer(TransportMessage message, TimeSpan delayBy, Address address)
        {
            
        }

        public void ClearDeferredMessages(string headerKey, string headerValue)
        {
            
        }
    }
}