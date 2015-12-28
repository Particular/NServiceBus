namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Config;
    using NServiceBus.Features;
    using NUnit.Framework;
    using ScenarioDescriptors;

    //repro for issue: https://github.com/NServiceBus/NServiceBus/issues/1020
    public class When_a_saga_message_goes_through_the_slr : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_invoke_the_correct_handle_methods_on_the_saga()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<SagaMsgThruSlrEndpt>(b => b
                    .When(bus => bus.SendLocal(new StartSagaMessage
                    {
                        SomeId = Guid.NewGuid()
                    })))
                .Done(c => c.SecondMessageProcessed)
                .Repeat(r => r.For(Transports.Default))
                .Run();
        }

        public class Context : ScenarioContext
        {
            public bool SecondMessageProcessed { get; set; }

            public int NumberOfTimesInvoked { get; set; }
        }

        public class SagaMsgThruSlrEndpt : EndpointConfigurationBuilder
        {
            public SagaMsgThruSlrEndpt()
            {
                EndpointSetup<DefaultServer>(b =>
                {
                    b.EnableFeature<TimeoutManager>();
                    b.EnableFeature<SecondLevelRetries>();
                }).WithConfig<SecondLevelRetriesConfig>(slr =>
                {
                    slr.NumberOfRetries = 1;
                    slr.TimeIncrease = TimeSpan.FromMilliseconds(1);
                });
            }

            public class TestSaga09 : Saga<TestSagaData09>, 
                IAmStartedByMessages<StartSagaMessage>,
                IHandleMessages<SecondSagaMessage>
            {
                public Context TestContext { get; set; }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    Data.SomeId = message.SomeId;

                    return context.SendLocal(new SecondSagaMessage
                    {
                        SomeId = Data.SomeId
                    });
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData09> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId)
                        .ToSaga(s=>s.SomeId);
                    mapper.ConfigureMapping<SecondSagaMessage>(m => m.SomeId)
                      .ToSaga(s => s.SomeId);
                }

                public Task Handle(SecondSagaMessage message, IMessageHandlerContext context)
                {
                    TestContext.NumberOfTimesInvoked++;

                    if(TestContext.NumberOfTimesInvoked < 2)
                        throw new SimulatedException();

                    TestContext.SecondMessageProcessed = true;

                    return Task.FromResult(0);
                }
            }

            public class TestSagaData09 : IContainSagaData
            {
                public virtual Guid Id { get; set; }
                public virtual string Originator { get; set; }
                public virtual string OriginalMessageId { get; set; }
                public virtual Guid SomeId { get; set; }
            }
        }

        [Serializable]
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