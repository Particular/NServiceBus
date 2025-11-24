namespace NServiceBus.AcceptanceTests.Sagas;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

//repro for issue: https://github.com/NServiceBus/NServiceBus/issues/1020
public class When_saga_message_goes_through_delayed_retries : NServiceBusAcceptanceTest
{
    [Test]
    public Task Should_invoke_the_correct_handle_methods_on_the_saga()
    {
        Requires.DelayedDelivery();

        return Scenario.Define<Context>()
            .WithEndpoint<DelayedRetryEndpoint>(b => b
                .When(session => session.SendLocal(new StartSagaMessage
                {
                    SomeId = Guid.NewGuid()
                })))
            .Done(c => c.SecondMessageProcessed)
            .Run();
    }

    public class Context : ScenarioContext
    {
        public bool SecondMessageProcessed { get; set; }
        public int NumberOfTimesInvoked { get; set; }
    }

    public class DelayedRetryEndpoint : EndpointConfigurationBuilder
    {
        public DelayedRetryEndpoint() =>
            EndpointSetup<DefaultServer>(b =>
            {
                var recoverability = b.Recoverability();
                recoverability.Delayed(settings =>
                {
                    settings.NumberOfRetries(1);
                    settings.TimeIncrease(TimeSpan.FromMilliseconds(1));
                });
            });

        public class DelayedRetryTestingSaga(Context testContext) : Saga<DelayedRetryTestingSagaData>,
            IAmStartedByMessages<StartSagaMessage>,
            IHandleMessages<SecondSagaMessage>
        {
            public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
            {
                Data.SomeId = message.SomeId;

                return context.SendLocal(new SecondSagaMessage
                {
                    SomeId = Data.SomeId
                });
            }

            public Task Handle(SecondSagaMessage message, IMessageHandlerContext context)
            {
                testContext.NumberOfTimesInvoked++;

                if (testContext.NumberOfTimesInvoked < 2)
                {
                    throw new SimulatedException();
                }

                testContext.SecondMessageProcessed = true;

                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<DelayedRetryTestingSagaData> mapper) =>
                mapper.MapSaga(s => s.SomeId)
                    .ToMessage<StartSagaMessage>(m => m.SomeId)
                    .ToMessage<SecondSagaMessage>(m => m.SomeId);
        }

        public class DelayedRetryTestingSagaData : ContainSagaData
        {
            public virtual Guid SomeId { get; set; }
        }
    }


    public class StartSagaMessage : ICommand
    {
        public Guid SomeId { get; set; }
    }

    public class SecondSagaMessage : ICommand
    {
        public Guid SomeId { get; set; }
    }
}