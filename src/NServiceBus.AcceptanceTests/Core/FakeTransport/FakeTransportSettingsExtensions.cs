namespace NServiceBus.AcceptanceTests.Core.FakeTransport
{
    using System;
    using Configuration.AdvancedExtensibility;

    public static class FakeTransportSettingsExtensions
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

        public static TransportExtensions<FakeTransport> WhenQueuesCreated(this TransportExtensions<FakeTransport> transportExtensions, Action action)
        {
            transportExtensions.GetSettings().Set("FakeTransport.QueueCreatorAction", action);

            return transportExtensions;
        }
    }
}