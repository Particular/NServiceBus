namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    //repro for issue: https://github.com/NServiceBus/NServiceBus/issues/1020
    public class When_saga_message_goes_through_delayed_retries : NServiceBusAcceptanceTest
    {
        [Test]
        public Task Should_invoke_the_correct_handle_methods_on_the_saga()
        {
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
            public DelayedRetryEndpoint()
            {
                EndpointSetup<DefaultServer>(b =>
                {
                    b.EnableFeature<TimeoutManager>();
                    var recoverability = b.Recoverability();
                    recoverability.Delayed(settings =>
                    {
                        settings.NumberOfRetries(1);
                        settings.TimeIncrease(TimeSpan.FromMilliseconds(1));
                    });
                });
            }

            public class DelayedRetryTestingSaga : Saga<DelayedRetryTestingSagaData>,
                IAmStartedByMessages<StartSagaMessage>,
                IHandleMessages<SecondSagaMessage>
            {
                public Context TestContext { get; set; }

                public DelayedRetryTestingSaga(Context testContext)
                {
                    TestContext = testContext;
                }

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
                    TestContext.NumberOfTimesInvoked++;

                    if (TestContext.NumberOfTimesInvoked < 2)
                    {
                        throw new SimulatedException();
                    }

                    TestContext.SecondMessageProcessed = true;

                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<DelayedRetryTestingSagaData> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                    mapper.ConfigureMapping<SecondSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                }
            }

            public class DelayedRetryTestingSagaData : IContainSagaData
            {
                public virtual Guid SomeId { get; set; }
                public virtual Guid Id { get; set; }
                public virtual string Originator { get; set; }
                public virtual string OriginalMessageId { get; set; }
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

        public class SomeTimeout
        {
        }
    }
}