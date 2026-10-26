namespace NServiceBus.AcceptanceTests.Registrations.Handlers;

using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NUnit.Framework;

public class When_registering_sagas_through_a_contributor : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_start_and_complete_the_saga_added_by_the_contributor()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointUsingContributor>(b => b
                .When(session => session.SendLocal(new StartContributedSaga { OrderId = "order-1" }))
                .When(c => c.SagaStarted, session => session.SendLocal(new CompleteContributedSaga { OrderId = "order-1" })))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.SagaStarted, Is.True);
            Assert.That(context.SagaCompleted, Is.True);
            Assert.That(context.CompletedSagaId, Is.EqualTo(context.StartedSagaId));
        }
    }

    public class Context : ScenarioContext
    {
        public bool SagaStarted { get; set; }
        public bool SagaCompleted { get; set; }
        public System.Guid StartedSagaId { get; set; }
        public System.Guid CompletedSagaId { get; set; }
    }

    public class EndpointUsingContributor : EndpointConfigurationBuilder
    {
        public EndpointUsingContributor() => EndpointSetup<NonScanningServer>(config => config.Handlers.All.ContributedSagaRegistrations.AddAll());

        [Saga]
        public class ContributedSaga(Context testContext) : Saga<ContributedSagaData>,
            IAmStartedByMessages<StartContributedSaga>,
            IHandleMessages<CompleteContributedSaga>
        {
            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<ContributedSagaData> mapper) =>
                mapper.MapSaga(s => s.OrderId)
                    .ToMessage<StartContributedSaga>(m => m.OrderId)
                    .ToMessage<CompleteContributedSaga>(m => m.OrderId);

            public Task Handle(StartContributedSaga message, IMessageHandlerContext context)
            {
                testContext.StartedSagaId = Data.Id;
                testContext.SagaStarted = true;
                return Task.CompletedTask;
            }

            public Task Handle(CompleteContributedSaga message, IMessageHandlerContext context)
            {
                testContext.CompletedSagaId = Data.Id;
                testContext.SagaCompleted = true;
                MarkAsComplete();
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }

        public class ContributedSagaData : ContainSagaData
        {
            public string OrderId { get; set; }
        }
    }

    public class StartContributedSaga : ICommand
    {
        public string OrderId { get; set; }
    }

    public class CompleteContributedSaga : ICommand
    {
        public string OrderId { get; set; }
    }
}
