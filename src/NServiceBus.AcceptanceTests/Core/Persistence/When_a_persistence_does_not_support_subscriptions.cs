namespace NServiceBus.AcceptanceTests.Core.Persistence
{
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using AcceptanceTesting.AcceptanceTestingPersistence;

    public class When_a_persistence_does_not_support_subscriptions : NServiceBusAcceptanceTest
    {
        [Test]
        public void should_throw_exception()
        {
            Requires.MessageDrivenPubSub();

            Assert.That(async () =>
            {
                await Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>(e => e.When(b => b.Subscribe<object>()))
                    .Run();
            }, Throws.Exception.With.Message.Contains("transportConfiguration.DisablePublishing()"));
        }

        class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<ServerWithNoDefaultPersistenceDefinitions>(c =>
                {
                    c.UsePersistence<AcceptanceTestingPersistence, StorageType.Sagas>();
                    c.UsePersistence<AcceptanceTestingPersistence, StorageType.Outbox>();
                    c.UsePersistence<AcceptanceTestingPersistence, StorageType.Timeouts>();
                });
            }
        }

        public class Context : ScenarioContext
        {
            public bool MessageReceived { get; set; }
        }
    }
}