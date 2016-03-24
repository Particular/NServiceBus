namespace NServiceBus.AcceptanceTests.Persistence
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NServiceBus.Persistence;
    using NUnit.Framework;

    public class When_a_persistence_does_not_support_gateway_dedup
    {
        [Test]
        public void should_throw_exception()
        {
            Assert.That(async () =>
            {
                await Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>(e => e.When(b => Task.FromResult(0)))
                    .Run();
            }, Throws.Exception.InnerException.InnerException.With.Message.Contains("DisableFeature<Gateway>()"));
        }

        class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<ServerWithNoDefaultPersistenceDefinitions>(c =>
                {
                    c.UsePersistence<InMemoryPersistence, StorageType.Sagas>();
                    c.UsePersistence<InMemoryPersistence, StorageType.Outbox>();
                    c.UsePersistence<InMemoryPersistence, StorageType.Timeouts>();
                    c.UsePersistence<InMemoryPersistence, StorageType.Subscriptions>();

                    c.EnableFeature<Gateway>();
                    c.EnableFeature<InMemoryGatewayPersistence>();
                });
            }
        }

        class Gateway : Feature
        {
            protected override void Setup(FeatureConfigurationContext context)
            {
            }
        }

        public class Context : ScenarioContext
        {
            public bool MessageReceived { get; set; }
        }
    }
}