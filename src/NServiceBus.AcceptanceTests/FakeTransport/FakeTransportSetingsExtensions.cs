namespace NServiceBus.AcceptanceTests.FakeTransport
{
    using System;
    using NServiceBus.Configuration.AdvanceExtensibility;

    public static class FakeTransportSetingsExtensions
    {
        public static TransportExtensions<FakeTransport> ThrowCritical(this TransportExtensions<FakeTransport> transportExtensions, Exception exception)
        {
            transportExtensions.GetSettings().Set("FakeTransport.ThrowCritical", exception);

            return transportExtensions;
        }
    }
}