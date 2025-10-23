namespace NServiceBus.AcceptanceTests.Outbox;

using System;
using AcceptanceTesting;
using Configuration.AdvancedExtensibility;
using EndpointTemplates;
using NServiceBus.Persistence;
using NUnit.Framework;

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
                // Disable publishing otherwise it would require a subscription storage since core acceptance tests
                // run with message-driven pub sub
                var routingSettings = new RoutingSettings<AcceptanceTestingTransport>(c.GetSettings());
                routingSettings.DisablePublishing();
                c.UsePersistence<FakeNoOutboxSupportPersistence>();
            });
    }

    class FakeNoOutboxSupportPersistence : PersistenceDefinition, IPersistenceDefinitionFactory<FakeNoOutboxSupportPersistence>
    {
        public static FakeNoOutboxSupportPersistence Create() => new();
    }
}