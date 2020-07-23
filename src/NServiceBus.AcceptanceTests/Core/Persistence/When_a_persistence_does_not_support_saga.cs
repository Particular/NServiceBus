namespace NServiceBus.AcceptanceTests.Core.Persistence
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_a_persistence_does_not_support_saga : NServiceBusAcceptanceTest
    {
        [Test]
        public void should_throw_exception()
        {
            Assert.That(async () =>
            {
                await Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>(e => e.When(b => b.SendLocal(new StartSaga())))
                    .Done(c => c.MessageReceived)
                    .Run();
            }, Throws.Exception.With.Message.Contains("DisableFeature<Sagas>()"));
        }

        class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<ServerWithNoDefaultPersistenceDefinitions>(c =>
                {
                    c.UsePersistence<InMemoryPersistence, StorageType.Timeouts>();
                    c.UsePersistence<InMemoryPersistence, StorageType.Outbox>();
                    c.UsePersistence<InMemoryPersistence, StorageType.Subscriptions>();
                });
            }
        }

        public class PersistenceSaga : Saga<PersistenceSaga.PersistenceSagaData>,
            IAmStartedByMessages<StartSaga>
        {
            public Context TestContext { get; set; }

            public Task Handle(StartSaga message, IMessageHandlerContext context)
            {
                TestContext.MessageReceived = true;
                MarkAsComplete();
                return Task.FromResult(0);
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<PersistenceSagaData> mapper)
            {
            }

            public class PersistenceSagaData : ContainSagaData
            {
                public virtual Guid DataId { get; set; }
            }
        }

        public class Context : ScenarioContext
        {
            public bool MessageReceived { get; set; }
        }

        public class StartSaga : ICommand
        {
        }
    }
}