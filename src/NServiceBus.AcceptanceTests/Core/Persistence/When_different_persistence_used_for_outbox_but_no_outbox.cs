namespace NServiceBus.AcceptanceTests.Core.Persistence
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NServiceBus.Persistence;
    using NUnit.Framework;

    public class When_different_persistence_used_for_outbox_but_no_outbox : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_not_throw() =>
            Assert.DoesNotThrowAsync(async () =>
            {
                await Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>(e => e.When(b => b.SendLocal(new StartSaga())))
                    .Run();
            });

        class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint() =>
                EndpointSetup<DefaultServer>(c =>
                {
                    c.UsePersistence<FakePersistence, StorageType.Outbox>();
                });

            public class NoOutboxSaga(Context testContext) : Saga<NoOutboxSaga.NoOutboxSagaData>,
                IAmStartedByMessages<StartSaga>
            {
                public Task Handle(StartSaga message, IMessageHandlerContext context)
                {
                    MarkAsComplete();
                    testContext.MarkAsCompleted();
                    return Task.CompletedTask;
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<NoOutboxSagaData> mapper) => mapper.MapSaga(x => x.DataId).ToMessage<StartSaga>(m => m.DataId);

                public class NoOutboxSagaData : ContainSagaData
                {
                    public virtual Guid DataId { get; set; }
                }
            }

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
        }

        public class Context : ScenarioContext;

        public class StartSaga : ICommand
        {
            public Guid DataId { get; set; }
        }
    }
}