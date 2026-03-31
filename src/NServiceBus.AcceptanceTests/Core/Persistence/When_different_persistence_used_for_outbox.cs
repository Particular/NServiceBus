namespace NServiceBus.AcceptanceTests.Core.Persistence
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NServiceBus.Persistence;
    using NUnit.Framework;

    public class When_different_persistence_used_for_outbox : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_throw_exception() =>
            Assert.That(async () =>
            {
                await Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>(e => e.When(b => b.SendLocal(new StartSaga())))
                    .Run();
            }, Throws.Exception.With.Message.Contains("Sagas and the Outbox need to use the same type of persistence."));

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint() =>
                EndpointSetup<DefaultServer>(c =>
                {
                    c.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;

                    c.EnableOutbox();
                    c.UsePersistence<FakePersistence, StorageType.Outbox>();
                });

            class FakePersistence : PersistenceDefinition, IPersistenceDefinitionFactory<FakePersistence>
            {
                FakePersistence()
                {
                    Supports<StorageType.Outbox, FakeOutboxStorage>();
                    Supports<StorageType.Sagas, FakeSagaStorage>();
                }

                static FakePersistence IPersistenceDefinitionFactory<FakePersistence>.Create() => new();

                sealed class FakeOutboxStorage : Feature
                {
                    public FakeOutboxStorage() => DependsOn<Outbox>();

                    protected override void Setup(FeatureConfigurationContext context)
                    {
                    }
                }

                sealed class FakeSagaStorage : Feature
                {
                    public FakeSagaStorage() => DependsOn<Sagas>();
                    protected override void Setup(FeatureConfigurationContext context)
                    {
                    }
                }
            }

            [Saga]
            public class DifferentPersistenceSaga(Context testContext) : Saga<DifferentPersistenceSaga.DifferentPersistenceSagaData>,
                IAmStartedByMessages<StartSaga>
            {
                public Task Handle(StartSaga message, IMessageHandlerContext context)
                {
                    testContext.MessageReceived = true;
                    MarkAsComplete();
                    testContext.MarkAsCompleted();
                    return Task.CompletedTask;
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<DifferentPersistenceSagaData> mapper) => mapper.MapSaga(x => x.DataId).ToMessage<StartSaga>(m => m.DataId);

                public class DifferentPersistenceSagaData : ContainSagaData
                {
                    public virtual Guid DataId { get; set; }
                }
            }
        }

        public class Context : ScenarioContext
        {
            public bool MessageReceived { get; set; }
        }

        public class StartSaga : ICommand
        {
            public Guid DataId { get; set; }
        }
    }
}