namespace NServiceBus.Core.Tests.Persistence;

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

        var supported = settings.HasSupportFor<StorageType.Subscriptions>();

        Assert.That(supported, Is.False);
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

        Assert.Throws<Exception>(() =>
        {
            config.Settings.ConfigurePersistence();
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

        Assert.DoesNotThrow(() => config.Settings.ConfigurePersistence(), "Should not throw for a single single feature enabled out of the two.");
    }

    class FakeSagaPersistence : PersistenceDefinition, IPersistenceDefinitionFactory<FakeSagaPersistence>
    {
        FakeSagaPersistence() => Supports<StorageType.Sagas, FakeStorage>();
        public static FakeSagaPersistence Create(SettingsHolder settings) => new();

        class FakeStorage : Feature
        {
            protected internal override void Setup(FeatureConfigurationContext context) => throw new System.NotImplementedException();
        }
    }

    class FakeOutboxPersistence : PersistenceDefinition, IPersistenceDefinitionFactory<FakeOutboxPersistence>
    {
        FakeOutboxPersistence() => Supports<StorageType.Outbox, FakeStorage>();
        public static FakeOutboxPersistence Create(SettingsHolder settings) => new();

        class FakeStorage : Feature
        {
            protected internal override void Setup(FeatureConfigurationContext context) => throw new System.NotImplementedException();
        }
    }
}
