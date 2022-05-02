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
        public void Should_throw_exception()
        {
            Assert.That(async () =>
            {
                await Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>(e => e.When(b => b.SendLocal(new StartSaga())))
                    .Done(c => c.MessageReceived)
                    .Run();
            }, Throws.Exception.With.Message.Contains("Sagas and the Outbox need to use the same type of persistence."));
        }

        class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.ConfigureTransport().Transactions(TransportTransactionMode.ReceiveOnly);

                    c.EnableOutbox();
                    c.UsePersistence<FakePersistence, StorageType.Outbox>();
                });
            }

            class FakePersistence : PersistenceDefinition
            {
                public FakePersistence()
                {
                    Supports<StorageType.Outbox>(s => s.EnableFeatureByDefault<Outbox>());
                    Supports<StorageType.Sagas>(s => s.EnableFeatureByDefault<Sagas>());
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
                    mapper.MapSaga(x => x.DataId).ToMessage<StartSaga>(m => m.DataId);
                }

                public class PersistenceSagaData : ContainSagaData
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