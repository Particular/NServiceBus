namespace NServiceBus.Core.Tests.Persistence
{
    using System;
    using System.Collections.Generic;
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

        [Test]
        public void Should_prevent_using_different_persistence_for_sagas_and_outbox()
        {
            var config = new EndpointConfiguration("MyEndpoint");
            config.UsePersistence<InMemoryPersistence>();
            config.UsePersistence<FakeSagaPersistence, StorageType.Sagas>();
            config.UsePersistence<FakeOutboxPersistence, StorageType.Outbox>();

            var startup = new PersistenceStartup();

            Assert.Throws<Exception>(() =>
            {
                startup.Run(config.Settings);
            }, "Sagas and Outbox need to use the same type of persistence. Saga is configured to use FakeSagaPersistence. Outbox is configured to use FakeOutboxPersistence");
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
