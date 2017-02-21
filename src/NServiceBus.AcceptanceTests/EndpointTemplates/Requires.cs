namespace NServiceBus.AcceptanceTests
{
    using NUnit.Framework;

    static class Requires
    {
        public static void DtcSupport()
        {
            if (!constraints.SupportsDtc)
            {
                Assert.Ignore("Ignoring this test because it requires DTC transaction support from the transport.");
            }
        }

        public static void CrossQueueTransactionSupport()
        {
            if (!constraints.SupportsCrossQueueTransactions)
            {
                Assert.Ignore("Ignoring this test because it requires cross queue transaction support from the transport.");
            }
        }

        public static void NativePubSubSupport()
        {
            if (!constraints.SupportsNativePubSub)
            {
                Assert.Ignore("Ignoring this test because it requires native publish subscribe support from the transport.");
            }
        }

        public static void MessageDrivenPubSub()
        {
            if (constraints.SupportsNativePubSub)
            {
                Assert.Ignore("Ignoring this test because it requires message driven publish subscribe but this test suite uses native publish subscribe.");
            }
        }

        public static void TimeoutStorage()
        {
            if (constraints.SupportsNativeDeferral)
            {
                Assert.Ignore("Ignoring this test because it requires the timeout manager but this transport provides native deferral.");
            }
        }

        public static void OutboxPersistence()
        {
            if (!constraints.SupportsOutbox)
            {
                Assert.Ignore("Ignoring this tests because it requires a persistence providing an Outbox storage.");
            }
        }

        static readonly TestSuiteConstraints constraints = new TestSuiteConstraints();
    }
}