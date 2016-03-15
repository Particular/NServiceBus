namespace NServiceBus.AcceptanceTests.Persistence
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Configuration.AdvanceExtensibility;
    using EndpointTemplates;
    using NServiceBus.Persistence;
    using NUnit.Framework;

    public class When_a_persistence_does_not_support_outbox
    {
        [Test]
        public void should_throw_exception()
        {
            Assert.That(async () =>
            {
                await Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>(e => e.When(b => Task.FromResult(0)))
                    .Run();
            }, Throws.Exception.InnerException.InnerException.With.Message.Contains("DisableFeature<Outbox>()"));
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

                    c.GetSettings().Set("DisableOutboxTransportCheck", true);
                    c.EnableOutbox();
                });
            }
        }

        public class Context : ScenarioContext
        {
            public bool MessageRecieved { get; set; }
        }
    }
}