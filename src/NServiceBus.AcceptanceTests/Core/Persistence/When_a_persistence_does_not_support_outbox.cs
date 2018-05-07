namespace NServiceBus.AcceptanceTests.Core.Persistence
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_a_persistence_does_not_support_outbox : NServiceBusAcceptanceTest
    {
        [Test]
        public void should_throw_exception()
        {
            Assert.That(async () =>
            {
                await Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>(e => e.When(b => Task.FromResult(0)))
                    .Run();
            }, Throws.Exception.With.Message.Contains("DisableFeature<Outbox>()"));
        }

        class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<ServerWithNoDefaultPersistenceDefinitions>(c =>
                {
                    c.UsePersistence<InMemoryPersistence, StorageType.Sagas>();
                    c.UsePersistence<InMemoryPersistence, StorageType.GatewayDeduplication>();
                    c.UsePersistence<InMemoryPersistence, StorageType.Timeouts>();
                    c.UsePersistence<InMemoryPersistence, StorageType.Subscriptions>();
                    
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