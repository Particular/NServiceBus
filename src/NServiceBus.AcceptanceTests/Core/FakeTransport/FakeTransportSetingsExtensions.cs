namespace NServiceBus.AcceptanceTests.Core.FakeTransport
{
    using System;
    using Configuration.AdvanceExtensibility;

    public static class FakeTransportSetingsExtensions
    {
        public static TransportExtensions<FakeTransport> RaiseCriticalErrorDuringStartup(this TransportExtensions<FakeTransport> transportExtensions, Exception exception)
        {
            transportExtensions.GetSettings().Set<Exception>(exception);

            return transportExtensions;
        }
    }
}