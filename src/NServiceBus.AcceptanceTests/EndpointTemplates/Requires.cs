namespace NServiceBus.AcceptanceTests
{
    using NUnit.Framework;

    static class Requires
    {
        public static void DtcSupport()
        {
            if (!constraints.SupportDtc)
            {
                Assert.Ignore("Ignoring this test because it requires DTC transaction support from the transport.");
            }
        }

        public static void CrossQueueTransactionSupport()
        {
            if (!constraints.SupportCrossQueueTransactions)
            {
                Assert.Ignore("Ignoring this test because it requires cross queue transaction support from the transport.");
            }
        }

        public static void NativePubSubSupport()
        {
            if (!constraints.SupportNativePubSub)
            {
                Assert.Ignore("Ignoring this test because it requires native publish subscribe support from the transport.");
            }
        }

        public static void MessageDrivenPubSub()
        {
            if (constraints.SupportNativePubSub)
            {
                Assert.Ignore("Ignoring this test because it requires message driven publish subscribe but this test suite uses native publish subscribe.");
            }
        }

        public static void TimeoutStorage()
        {
            if (constraints.SupportNativeDeferral)
            {
                Assert.Ignore("Ignoring this test because it requires the timeout manager but this transport provides native deferral.");
            }
        }

        public static void OutboxPersistence()
        {
            if (!constraints.ProvideOutboxPersistence)
            {
                Assert.Ignore("Ignoring this tests because it requires a persistence providing an Outbox storage.");
            }
        }

        static readonly TestSuiteConstraints constraints = new TestSuiteConstraints();
    }
}