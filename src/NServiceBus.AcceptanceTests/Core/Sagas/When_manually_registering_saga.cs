namespace NServiceBus.AcceptanceTests.Core.Sagas;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_manually_registering_saga : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_handle_message_with_manually_registered_saga()
    {
        var id = Guid.NewGuid();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<ManualSagaEndpoint>(b => b.When(session => session.SendLocal(new StartManualSaga
            {
                OrderId = id
            })))
            .Done(c => c.SagaWasInvoked)
            .Run();

        Assert.That(context.SagaWasInvoked, Is.True);
        Assert.That(context.OrderId, Is.EqualTo(id));
    }

    public class Context : ScenarioContext
    {
        public bool SagaWasInvoked { get; set; }
        public Guid OrderId { get; set; }
    }

    public class ManualSagaEndpoint : EndpointConfigurationBuilder
    {
        public ManualSagaEndpoint()
        {
            EndpointSetup<DefaultServer>(config =>
            {
                config.AddSaga<ManuallyRegisteredSaga>();
            });
        }

        public class ManuallyRegisteredSaga(Context testContext)
            : Saga<ManuallyRegisteredSagaData>, IAmStartedByMessages<StartManualSaga>
        {
            public Task Handle(StartManualSaga message, IMessageHandlerContext context)
            {
                testContext.SagaWasInvoked = true;
                testContext.OrderId = Data.OrderId;
                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<ManuallyRegisteredSagaData> mapper) =>
                mapper.MapSaga(s => s.OrderId)
                    .ToMessage<StartManualSaga>(m => m.OrderId);
        }

        public class ManuallyRegisteredSagaData : ContainSagaData
        {
            public virtual Guid OrderId { get; set; }
        }
    }

    public class StartManualSaga : ICommand
    {
        public Guid OrderId { get; set; }
    }
}

