namespace NServiceBus.AcceptanceTests.Core.Persistence
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_a_persistence_does_not_support_outbox : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_throw_exception()
        {
            Assert.That(async () =>
            {
                await Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>(e => e.When(b => Task.CompletedTask))
                    .Run();
            }, Throws.Exception.With.Message.Contains("DisableFeature<Outbox>()"));
        }

        class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<ServerWithNoDefaultPersistenceDefinitions>(c =>
                {
                    c.UsePersistence<AcceptanceTestingPersistence, StorageType.Sagas>();
                    c.UsePersistence<AcceptanceTestingPersistence, StorageType.Subscriptions>();

                    c.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;
                    c.EnableOutbox();
                });
            }
        }

        public class Context : ScenarioContext
        {
            public bool MessageReceived { get; set; }
        }
    }
}