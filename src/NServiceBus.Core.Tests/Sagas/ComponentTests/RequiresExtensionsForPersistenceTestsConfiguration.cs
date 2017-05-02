namespace NServiceBus.Persistence.ComponentTests
{
    using NUnit.Framework;

    public static class RequiresExtensionsForPersistenceTestsConfiguration
    {
        public static void RequiresDtcSupport(this IPersistenceTestsConfiguration configuration)
        {
            if (!configuration.SupportsDtc)
            {
                Assert.Ignore("Ignoring this test because it requires DTC transaction support from persister.");
            }
        }

        public static void RequiresOutboxSupport(this IPersistenceTestsConfiguration configuration)
        {
            if (!configuration.SupportsOutbox)
            {
                Assert.Ignore("Ignoring this test because it requires outbox support from persister.");
            }
        }

        public static void RequiresFindersSupport(this IPersistenceTestsConfiguration configuration)
        {
            if (!configuration.SupportsFinders)
            {
                Assert.Ignore("Ignoring this test because it requires custom finder support from persister.");
            }
        }

        public static void RequiresSubscriptionSupport(this IPersistenceTestsConfiguration configuration)
        {
            if (!configuration.SupportsSubscriptions)
            {
                Assert.Ignore("Ignoring this test because it requires subscription support from persister.");
            }
        }

        public static void RequiresTimeoutSupport(this IPersistenceTestsConfiguration configuration)
        {
            if (!configuration.SupportsTimeouts)
            {
                Assert.Ignore("Ignoring this test because it requires timout support from persister.");
            }
        }
    }
}