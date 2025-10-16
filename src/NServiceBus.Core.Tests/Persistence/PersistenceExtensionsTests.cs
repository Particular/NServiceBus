namespace NServiceBus.Core.Tests.Persistence;

using System;
using NServiceBus.Features;
using NServiceBus.Persistence;
using Settings;
using NUnit.Framework;

[TestFixture]
public class When_configuring_storage_type_not_supported_by_persistence
{
    [Test]
    public void Should_throw_exception()
    {
        var ex = Assert.Throws<Exception>(() => new PersistenceExtensions<PartialPersistence, StorageType.Sagas>(new SettingsHolder()));
        Assert.That(ex.Message, Does.StartWith("PartialPersistence does not support storage type Sagas."));
    }

    public class PartialPersistence : PersistenceDefinition, IPersistenceDefinitionFactory<PartialPersistence>
    {
        PartialPersistence() => Supports<StorageType.Subscriptions, FakeSubscriptionStorage>();

        public static PartialPersistence Create() => new();

        class FakeSubscriptionStorage : Feature
        {
            protected internal override void Setup(FeatureConfigurationContext context) => throw new NotImplementedException();
        }
    }
}

[TestFixture]
public class When_configuring_storage_type_supported_by_persistence
{
    [Test]
    public void Should_not_throw_exception()
    {
        Assert.DoesNotThrow(() => new PersistenceExtensions<PartialPersistence, StorageType.Subscriptions>(new SettingsHolder()));
    }

    public class PartialPersistence : PersistenceDefinition, IPersistenceDefinitionFactory<PartialPersistence>
    {
        public PartialPersistence() => Supports<StorageType.Subscriptions, FakeSubscriptionStorage>();

        public static PartialPersistence Create() => new();

        class FakeSubscriptionStorage : Feature
        {
            protected internal override void Setup(FeatureConfigurationContext context) => throw new NotImplementedException();
        }
    }
}