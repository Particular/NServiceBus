namespace NServiceBus.AcceptanceTests.Core.FakeTransport
{
    using System;
    using Configuration.AdvanceExtensibility;

    public static class FakeTransportSetingsExtensions
    {
        public static TransportExtensions<FakeTransport> RaiseCriticalErrorDuringStartup(this TransportExtensions<FakeTransport> transportExtensions, Exception exception)
        {
            transportExtensions.GetSettings().Set<Exception>(exception);
            transportExtensions.GetSettings().Set("FakeTransport.ThrowCritical", true);

            return transportExtensions;
        }

        public static TransportExtensions<FakeTransport> RaiseExceptionDuringPumpStop(this TransportExtensions<FakeTransport> transportExtensions, Exception exception)
        {
            transportExtensions.GetSettings().Set<Exception>(exception);
            transportExtensions.GetSettings().Set("FakeTransport.ThrowOnPumpStop", true);

            return transportExtensions;
        }

        public static TransportExtensions<FakeTransport> RaiseExceptionDuringInfrastructureStop(this TransportExtensions<FakeTransport> transportExtensions, Exception exception)
        {
            transportExtensions.GetSettings().Set<Exception>(exception);
            transportExtensions.GetSettings().Set("FakeTransport.ThrowOnInfrastructureStop", true);

            return transportExtensions;
        }
    }
}