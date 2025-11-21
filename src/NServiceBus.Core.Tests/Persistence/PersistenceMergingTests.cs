namespace NServiceBus.Core.Tests.Persistence;

using System.Linq;
using NServiceBus.Features;
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

        var enabledPersistences = config.Settings.Get<PersistenceComponent.Settings>().Enabled;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(enabledPersistences, Has.Count.EqualTo(1));
            Assert.That(enabledPersistences.ElementAt(0).SelectedStorages,
                Is.EquivalentTo(StorageType.GetAvailableStorageTypes().Select(x => (x, x.Defaults))));
        }
    }

    class FakePersistence : PersistenceDefinition, IPersistenceDefinitionFactory<FakePersistence>
    {
        FakePersistence()
        {
            Supports<StorageType.Sagas, FakeStorage>();
            Supports<StorageType.Outbox, FakeStorage>();
            Supports<StorageType.Subscriptions, FakeStorage>();
        }

        public static FakePersistence Create() => new();

        class FakeStorage : Feature
        {
            protected override void Setup(FeatureConfigurationContext context) =>
                throw new System.NotImplementedException();
        }
    }
}

[TestFixture]
public class When_options_are_provided
{
    [Test]
    public void Should_use_options_provided_by_persistence()
    {
        var config = new EndpointConfiguration("MyEndpoint");
        config.UsePersistence<FakePersistence>();

        var enabledPersistences = config.Settings.Get<PersistenceComponent.Settings>().Enabled;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(enabledPersistences, Has.Count.EqualTo(1));
            Assert.That(enabledPersistences.ElementAt(0).SelectedStorages,
                Is.EquivalentTo(new (StorageType, StorageType.Options Options)[]
                {
                    (new StorageType.Sagas(), new StorageType.SagasOptions { SupportsFinders = true }),
                    (new StorageType.Subscriptions(), new StorageType.Options()),
                    (new StorageType.Outbox(), new StorageType.Options())
                }));
        }
    }

    class FakePersistence : PersistenceDefinition, IPersistenceDefinitionFactory<FakePersistence>
    {
        FakePersistence()
        {
            Supports<StorageType.Sagas, FakeStorage>(new StorageType.SagasOptions { SupportsFinders = true });
            Supports<StorageType.Outbox, FakeStorage>();
            Supports<StorageType.Subscriptions, FakeStorage>();
        }

        public static FakePersistence Create() => new();

        class FakeStorage : Feature
        {
            protected override void Setup(FeatureConfigurationContext context) =>
                throw new System.NotImplementedException();
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

        var enabledPersistences = config.Settings.Get<PersistenceComponent.Settings>().Enabled;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(enabledPersistences, Has.Count.EqualTo(2));
            Assert.That(enabledPersistences.ElementAt(0).SelectedStorages,
                Is.EquivalentTo(
                    new StorageType[] { new StorageType.Sagas(), new StorageType.Subscriptions() }.Select(x =>
                        (x, x.Defaults))));
            Assert.That(enabledPersistences.ElementAt(1).SelectedStorages,
                Is.EquivalentTo(new StorageType[] { new StorageType.Outbox() }.Select(x => (x, x.Defaults))));
        }
    }

    class FakePersistence2 : PersistenceDefinition, IPersistenceDefinitionFactory<FakePersistence2>
    {
        FakePersistence2()
        {
            Supports<StorageType.Sagas, FakeStorage>();
            Supports<StorageType.Subscriptions, FakeStorage>();
        }

        public static FakePersistence2 Create() => new();

        class FakeStorage : Feature
        {
            protected override void Setup(FeatureConfigurationContext context) =>
                throw new System.NotImplementedException();
        }
    }

    class FakePersistence : PersistenceDefinition, IPersistenceDefinitionFactory<FakePersistence>
    {
        FakePersistence()
        {
            Supports<StorageType.Sagas, FakeStorage>();
            Supports<StorageType.Outbox, FakeStorage>();
            Supports<StorageType.Subscriptions, FakeStorage>();
        }

        public static FakePersistence Create() => new();

        class FakeStorage : Feature
        {
            protected override void Setup(FeatureConfigurationContext context) =>
                throw new System.NotImplementedException();
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

        var enabledPersistences = config.Settings.Get<PersistenceComponent.Settings>().Enabled;

        Assert.That(enabledPersistences.Any(p => p.SelectedStorages.Contains<StorageType.Subscriptions>()), Is.False);
    }

    class FakePersistence : PersistenceDefinition, IPersistenceDefinitionFactory<FakePersistence>
    {
        FakePersistence()
        {
            Supports<StorageType.Sagas, FakeStorage>();
            Supports<StorageType.Subscriptions, FakeStorage>();
        }

        public static FakePersistence Create() => new();

        class FakeStorage : Feature
        {
            protected override void Setup(FeatureConfigurationContext context) =>
                throw new System.NotImplementedException();
        }
    }
}