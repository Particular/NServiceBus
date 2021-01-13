namespace NServiceBus.Core.Tests.Persistence
{
    using NServiceBus.Core.Tests.Fakes;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Persistence;
    using NUnit.Framework;

    [TestFixture]
    public class When_no_storage_persistence_overrides_are_enabled
    {
        [Test]
        public void Should_use_all_storages_supported_by_persistence()
        {
            var config = new EndpointConfiguration("MyEndpoint");
            config.UsePersistence<FakePersistence>();
            var persistences = config.Settings.Get<List<EnabledPersistence>>("PersistenceDefinitions");

            var resultedEnabledPersistences = PersistenceStorageMerger.Merge(persistences, config.Settings);

            Assert.That(resultedEnabledPersistences[0].SelectedStorages, Is.EquivalentTo(StorageType.GetAvailableStorageTypes()));
        }

        class FakePersistence : PersistenceDefinition
        {
            public FakePersistence()
            {
                Supports<StorageType.Sagas>(settings => { });
                Supports<StorageType.Outbox>(settings => { });
                Supports<StorageType.Subscriptions>(settings => { });
            }
        }
    }

    [TestFixture]
    public class When_storage_overrides_are_provided
    {
        [Test]
        public void Should_replace_default_storages_by_overrides()
        {
            var config = new EndpointConfiguration("MyEndpoint");
            config.UsePersistence<FakePersistence>();
            config.UsePersistence<FakePersistence2, StorageType.Sagas>();
            config.UsePersistence<FakePersistence2, StorageType.Subscriptions>();
            var persistences = config.Settings.Get<List<EnabledPersistence>>("PersistenceDefinitions");

            var resultedEnabledPersistences = PersistenceStorageMerger.Merge(persistences, config.Settings);

            Assert.That(resultedEnabledPersistences[0].SelectedStorages, Is.EquivalentTo(
                new List<Type> {typeof(StorageType.Subscriptions)}));
            Assert.That(resultedEnabledPersistences[1].SelectedStorages, Is.EquivalentTo(
                new List<Type> {typeof(StorageType.Sagas)}));
        }

        class FakePersistence2 : PersistenceDefinition
        {
            public FakePersistence2()
            {
                Supports<StorageType.Sagas>(settings => { });
                Supports<StorageType.Subscriptions>(settings => { });
            }
        }

        class FakePersistence : PersistenceDefinition
        {
            public FakePersistence()
            {
                Supports<StorageType.Sagas>(settings => { });
                Supports<StorageType.Outbox>(settings => { });
                Supports<StorageType.Subscriptions>(settings => { });
            }
        }
    }

    [TestFixture]
    public class When_explicitly_enabling_selected_storage
    {
        [Test]
        public void Should_not_use_other_supported_storages()
        {
            var config = new EndpointConfiguration("MyEndpoint");
            config.UsePersistence<FakePersistence, StorageType.Sagas>();
            var persistences = config.Settings.Get<List<EnabledPersistence>>("PersistenceDefinitions");

            var resultedEnabledPersistences = PersistenceStorageMerger.Merge(persistences, config.Settings);

            Assert.IsFalse(resultedEnabledPersistences.Any(p => p.SelectedStorages.Contains(typeof(StorageType.Subscriptions))));
}

        class FakePersistence : PersistenceDefinition
        {
            public FakePersistence()
            {
                Supports<StorageType.Sagas>(settings => { });
                Supports<StorageType.Subscriptions>(settings => { });
            }
        }
    }
}
