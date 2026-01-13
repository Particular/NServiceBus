namespace NServiceBus.AcceptanceTests.Sagas;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Support;
using EndpointTemplates;
using NUnit.Framework;

public class When_updating_existing_correlation_property : NServiceBusAcceptanceTest
{
    [Test]
    public void Should_blow_up()
    {
        var exception = Assert.ThrowsAsync<MessageFailedException>(async () =>
            await Scenario.Define<Context>()
                .WithEndpoint<ChangePropertyEndpoint>(b => b.When(session => session.SendLocal(new StartSagaMessage
                {
                    SomeId = Guid.NewGuid()
                })))
                .Run());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(((Context)exception.ScenarioContext).ModifiedCorrelationProperty, Is.True);
            Assert.That(exception.ScenarioContext.FailedMessages, Has.Count.EqualTo(1));
        }
        Assert.That(
            exception.FailedMessage.Exception.Message,
            Does.Contain("Changing the value of correlated properties at runtime is currently not supported"));
    }

    public class Context : ScenarioContext
    {
        public bool ModifiedCorrelationProperty { get; set; }
    }

    public class ChangePropertyEndpoint : EndpointConfigurationBuilder
    {
        public ChangePropertyEndpoint() => EndpointSetup<DefaultServer>();

        public class ChangeCorrPropertySaga(Context testContext)
            : Saga<ChangeCorrPropertySagaData>, IAmStartedByMessages<StartSagaMessage>
        {
            public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
            {
                if (message.SecondMessage)
                {
                    Data.SomeId = Guid.NewGuid(); //this is not allowed
                    testContext.ModifiedCorrelationProperty = true;
                    return Task.CompletedTask;
                }

                return context.SendLocal(new StartSagaMessage
                {
                    SecondMessage = true,
                    SomeId = Data.SomeId
                });
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<ChangeCorrPropertySagaData> mapper) =>
                mapper.MapSaga(s => s.SomeId)
                    .ToMessage<StartSagaMessage>(m => m.SomeId);
        }

        public class ChangeCorrPropertySagaData : ContainSagaData
        {
            public virtual Guid SomeId { get; set; }
        }
    }

    public class StartSagaMessage : ICommand
    {
        public Guid SomeId { get; set; }
        public bool SecondMessage { get; set; }
    }
}