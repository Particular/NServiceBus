namespace NServiceBus.AcceptanceTests.Core.Persistence;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_a_persistence_does_not_support_saga : NServiceBusAcceptanceTest
{
    [Test]
    public void Should_throw_exception() =>
        Assert.That(async () =>
        {
            await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(e => e.When(b => b.SendLocal(new StartSaga())))
                .Run();
        }, Throws.Exception.With.Message.Contains("DisableFeature<Sagas>()"));

    class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() =>
            EndpointSetup<ServerWithNoDefaultPersistenceDefinitions>(c =>
            {
                c.UsePersistence<AcceptanceTestingPersistence, StorageType.Outbox>();
                c.UsePersistence<AcceptanceTestingPersistence, StorageType.Subscriptions>();
            });
    }

    public class SagaWithPersistenceNotSupportingIt : Saga<SagaWithPersistenceNotSupportingIt.SagaWithPersistenceNotSupportingItSagaData>,
        IAmStartedByMessages<StartSaga>
    {
        public Context TestContext { get; set; }

        public Task Handle(StartSaga message, IMessageHandlerContext context)
        {
            MarkAsComplete();
            TestContext.MarkAsCompleted();
            return Task.CompletedTask;
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaWithPersistenceNotSupportingItSagaData> mapper) =>
            mapper.MapSaga(s => s.DataId)
                .ToMessage<StartSaga>(m => m.DataId);

        public class SagaWithPersistenceNotSupportingItSagaData : ContainSagaData
        {
            public virtual Guid DataId { get; set; }
        }
    }

    public class Context : ScenarioContext;

    public class StartSaga : ICommand
    {
        public Guid DataId { get; set; }
    }
}