namespace NServiceBus.Core.Tests.Persistence
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Persistence;
    using NServiceBus.Settings;
    using NUnit.Framework;

    [TestFixture]
    public class When_no_storage_persistence_overrides_are_enabled
    {
        [Test]
        public void Should_use_all_storages_supported_by_persistence()
        {
            var settingsHolder = new SettingsHolder();
            var userProvidedEnabledPersistences = new List<EnabledPersistence>
            {
                new EnabledPersistence
                {
                    DefinitionType = typeof(InMemoryPersistence),
                    SelectedStorages = new List<Type>()
                }
            };

            var resultedEnabledPersistences = PersistenceStorageMerger.Merge(userProvidedEnabledPersistences, settingsHolder);

            Assert.That(resultedEnabledPersistences[0].SelectedStorages, Is.EquivalentTo(StorageType.GetAvailableStorageTypes()));
        }
    }

    [TestFixture]
    public class When_storage_overrides_are_provided
    {
        [Test]
        public void Should_replace_default_storages_by_overrides()
        {
            var settingsHolder = new SettingsHolder();
            var userProvidedEnabledPersistences = new List<EnabledPersistence>
            {
                new EnabledPersistence
                {
                    DefinitionType = typeof(InMemoryPersistence),
                    SelectedStorages = new List<Type>()
                },
                // user provided overrides
                new EnabledPersistence
                {
                    DefinitionType = typeof(FakePersistence),
                    SelectedStorages = new List<Type>{ typeof(StorageType.Sagas), typeof(StorageType.Subscriptions) }
                }
            };

            var resultedEnabledPersistences = PersistenceStorageMerger.Merge(userProvidedEnabledPersistences, settingsHolder);
            
            Assert.That(resultedEnabledPersistences[0].SelectedStorages, Is.EquivalentTo(
                new List<Type> { typeof(StorageType.Sagas), typeof(StorageType.Subscriptions) }));
            Assert.That(resultedEnabledPersistences[1].SelectedStorages, Is.EquivalentTo(
                new List<Type> { typeof(StorageType.GatewayDeduplication), typeof(StorageType.Outbox), typeof(StorageType.Timeouts) }));
        }

        class FakePersistence : PersistenceDefinition
        {
            public FakePersistence()
            {
                Supports<StorageType.Sagas>(settings => { });
                Supports<StorageType.Subscriptions>(settings => { });
                Supports<StorageType.Timeouts>(settings => { });
            }
        }
    }
}