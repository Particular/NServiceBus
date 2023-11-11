namespace NServiceBus.AcceptanceTests.Outbox;

using System;
using AcceptanceTesting;
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

        StringAssert.Contains("The selected persistence doesn't have support for outbox storage", exception.Message);
    }

    public class Context : ScenarioContext
    {
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint()
        {
            EndpointSetup<ServerWithNoDefaultPersistenceDefinitions>(c =>
            {
                c.EnableOutbox();
                c.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;
                c.UsePersistence<FakeNoOutboxSupportPersistence>();
            });
        }
    }

    class FakeNoOutboxSupportPersistence : PersistenceDefinition
    {
    }
}