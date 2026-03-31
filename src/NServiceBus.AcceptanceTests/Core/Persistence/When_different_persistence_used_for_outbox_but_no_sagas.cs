namespace NServiceBus.AcceptanceTests.Core.Persistence
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NServiceBus.Persistence;
    using NUnit.Framework;

    public class When_different_persistence_used_for_outbox_but_no_sagas : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_not_throw() =>
            Assert.DoesNotThrowAsync(async () =>
            {
                await Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>(e => e.When(b => b.SendLocal(new MyMessage())))
                    .Run();
            });

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint() =>
                EndpointSetup<DefaultServer>(c =>
                {
                    c.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;

                    c.EnableOutbox();
                    c.UsePersistence<FakePersistence, StorageType.Outbox>();
                });

            [Handler]
            public class MyHandler(Context testContext) : IHandleMessages<MyMessage>
            {
                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    testContext.MarkAsCompleted();
                    return Task.CompletedTask;
                }
            }

            public class FakePersistence : PersistenceDefinition, IPersistenceDefinitionFactory<FakePersistence>
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

        public class MyMessage : ICommand
        {
            public Guid DataId { get; set; }
        }
    }
}