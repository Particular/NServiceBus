namespace NServiceBus.AcceptanceTests.Outbox;

using System;
using AcceptanceTesting;
using EndpointTemplates;
using Features;
using NUnit.Framework;
using Persistence;

public class When_outbox_enabled_without_persister_supporting_it : NServiceBusAcceptanceTest
{
    [Test]
    public void Should_fail_to_start()
    {
        var exception = Assert.ThrowsAsync<Exception>(async () => await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>()
            .Done(_ => false)
            .Run());

        Assert.That(exception.Message, Does.Contain("The selected persistence doesn't have support for outbox storage"));
    }

    public class Context : ScenarioContext;

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() =>
            EndpointSetup<ServerWithNoDefaultPersistenceDefinitions>(c =>
            {
                c.EnableOutbox();
                c.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;
                c.UsePersistence<FakeNoOutboxSupportPersistence>();
            });
    }

    class FakeNoOutboxSupportPersistence : PersistenceDefinition, IPersistenceDefinitionFactory<FakeNoOutboxSupportPersistence>
    {
        FakeNoOutboxSupportPersistence() => Supports<StorageType.Subscriptions, SubscriptionStorage>();

        public static FakeNoOutboxSupportPersistence Create() => new();

        // This storage type is required because Core acceptance tests run with message-driven pub sub
        sealed class SubscriptionStorage : Feature, IFeatureFactory
        {
            protected override void Setup(FeatureConfigurationContext context) => throw new NotImplementedException();

            static Feature IFeatureFactory.Create() => new SubscriptionStorage();
        }
    }
}