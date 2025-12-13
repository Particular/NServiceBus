namespace NServiceBus.AcceptanceTests.Core.Sagas;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_saga_scanned_send_only_and_no_saga_storage : NServiceBusAcceptanceTest
{
    [Test]
    public void Should_not_throw() =>
        Assert.DoesNotThrowAsync(async () =>
        {
            await Scenario.Define<Context>()
                .WithEndpoint<SendOnlyEndpointWithSaga>()
                
                .Run();
        });

    class SendOnlyEndpointWithSaga : EndpointConfigurationBuilder
    {
        public SendOnlyEndpointWithSaga() =>
            EndpointSetup<ServerWithNoDefaultPersistenceDefinitions>(c =>
            {
                c.UsePersistence<AcceptanceTestingPersistence, StorageType.Subscriptions>();

                c.SendOnly();
            });
    }

    public class SagaInSendOnlyEndpoint : Saga<SagaInSendOnlyEndpoint.SagaInSendOnlyEndpointSagaData>, IAmStartedByMessages<StartMessage>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaInSendOnlyEndpointSagaData> mapper)
            => mapper.MapSaga(s => s.DataId).ToMessage<StartMessage>(m => m.SomeId);

        public class SagaInSendOnlyEndpointSagaData : ContainSagaData
        {
            public virtual Guid DataId { get; set; }
        }

        public Task Handle(StartMessage message, IMessageHandlerContext context) => Task.CompletedTask;
    }

    public class Context : ScenarioContext;

    public class StartMessage : ICommand
    {
        public Guid SomeId { get; set; }
    }
}