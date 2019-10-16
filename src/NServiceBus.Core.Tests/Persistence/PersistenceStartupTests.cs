namespace NServiceBus.Core.Tests.Persistence
{
    using System;
    using NServiceBus.Features;
    using NServiceBus.Persistence;
    using NUnit.Framework;
    using Settings;

    [TestFixture]
    public class When_no_persistence_has_been_configured
    {
        [Test]
        public void Should_return_false_when_checking_if_persistence_supports_storage_type()
        {
            var settings = new SettingsHolder();

            var supported = PersistenceStartup.HasSupportFor<StorageType.Subscriptions>(settings);

            Assert.IsFalse(supported);
        }
    }

    [TestFixture]
    public class When_persistence_has_been_configured
    {
        [Test]
        public void Should_prevent_using_different_persistence_for_sagas_and_outbox_for_both_features_enabled()
        {
            var config = new EndpointConfiguration("MyEndpoint");
            config.UsePersistence<FakeSagaPersistence, StorageType.Sagas>();
            config.UsePersistence<FakeOutboxPersistence, StorageType.Outbox>();

            config.EnableFeature<Outbox>();
            config.EnableFeature<Sagas>();

            var startup = new PersistenceStartup();

            Assert.Throws<Exception>(() =>
            {
                startup.Run(config.Settings);
            }, "Sagas and Outbox need to use the same type of persistence. Saga is configured to use FakeSagaPersistence. Outbox is configured to use FakeOutboxPersistence");
        }

        [TestCase(true, false)]
        [TestCase(false, true)]
        public void Should_not_prevent_using_different_persistence_for_sagas_and_outbox_if_only_one_of_the_features_is_enabled(bool sagasEnabled, bool outboxEnabled)
        {
            var config = new EndpointConfiguration("MyEndpoint");
            config.UsePersistence<FakeSagaPersistence, StorageType.Sagas>();
            config.UsePersistence<FakeOutboxPersistence, StorageType.Outbox>();

            if (sagasEnabled)
            {
                config.EnableFeature<Sagas>();
            }

            if (outboxEnabled)
            {
                config.EnableFeature<Outbox>();
            }

            var startup = new PersistenceStartup();

            Assert.DoesNotThrow(() => startup.Run(config.Settings), "Should not throw for a single single feature enabled out of the two.");
        }

        class FakeSagaPersistence : PersistenceDefinition
        {
            public FakeSagaPersistence()
            {
                Supports<StorageType.Sagas>(settings => { });
            }
        }

        class FakeOutboxPersistence : PersistenceDefinition
        {
            public FakeOutboxPersistence()
            {
                Supports<StorageType.Outbox>(settings => { });
            }
        }
    }
}
