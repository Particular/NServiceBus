namespace NServiceBus.AcceptanceTests.FakeTransport
{
    using System;

    public static class FakeTransportSetingsExtensions
    {
        public static TransportExtensions<FakeTransport> RaiseCriticalErrorDuringStartup(this TransportExtensions<FakeTransport> transportExtensions, Exception exception)
        {
            transportExtensions.Settings.Set(exception);

            return transportExtensions;
        }
    }
}