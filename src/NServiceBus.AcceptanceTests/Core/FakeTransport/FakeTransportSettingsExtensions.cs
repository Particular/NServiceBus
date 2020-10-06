namespace NServiceBus.AcceptanceTests.Core.FakeTransport
{
    using System;
    using Configuration.AdvancedExtensibility;
    using Settings;
    using Transport;

    public static class FakeTransportSettingsExtensions
    {
        public static TransportExtensions<FakeTransport> RaiseCriticalErrorDuringStartup(this TransportExtensions<FakeTransport> transportExtensions, Exception exception)
        {
            transportExtensions.GetSettings().Set(exception);
            transportExtensions.GetSettings().Set("FakeTransport.ThrowCritical", true);

            return transportExtensions;
        }

        public static TransportExtensions<FakeTransport> RaiseExceptionDuringPumpStop(this TransportExtensions<FakeTransport> transportExtensions, Exception exception)
        {
            transportExtensions.GetSettings().Set(exception);
            transportExtensions.GetSettings().Set("FakeTransport.ThrowOnPumpStop", true);

            return transportExtensions;
        }

        public static TransportExtensions<FakeTransport> RaiseExceptionDuringInfrastructureStop(this TransportExtensions<FakeTransport> transportExtensions, Exception exception)
        {
            transportExtensions.GetSettings().Set(exception);
            transportExtensions.GetSettings().Set("FakeTransport.ThrowOnInfrastructureStop", true);

            return transportExtensions;
        }

        public static TransportExtensions<FakeTransport> WhenQueuesCreated(this TransportExtensions<FakeTransport> transportExtensions, Action<QueueBindings> onQueueCreation)
        {
            transportExtensions.GetSettings().Set("FakeTransport.onQueueCreation", onQueueCreation);

            return transportExtensions;
        }

        public static TransportExtensions<FakeTransport> AssertSettings(this TransportExtensions<FakeTransport> transportExtensions, Action<ReadOnlySettings> assertion)
        {
            transportExtensions.GetSettings().Set("FakeTransport.AssertSettings", assertion);

            return transportExtensions;
        }
    }
}