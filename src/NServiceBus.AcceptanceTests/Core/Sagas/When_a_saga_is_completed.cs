namespace NServiceBus.AcceptanceTests.Core.Sagas
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    public class When_a_saga_is_completed : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Saga_should_not_handle_subsequent_messages_for_that_sagadata()
        {
            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                .WithEndpoint<SagaIsCompletedEndpoint>(b =>
                {
                    b.When((session, c) => session.SendLocal(new StartSagaMessage
                    {
                        SomeId = c.Id
                    }));
                    b.When(c => c.StartSagaMessageReceived, (session, c) => session.SendLocal(new CompleteSagaMessage
                    {
                        SomeId = c.Id
                    }));
                    b.When(c => c.SagaCompleted, (session, c) => session.SendLocal(new AnotherMessage
                    {
                        SomeId = c.Id
                    }));
                })
                .Done(c => c.AnotherMessageReceived)
                .Run();

            Assert.True(context.AnotherMessageReceived, "AnotherMessage should have been delivered to the handler outside the saga");
            Assert.False(context.SagaReceivedAnotherMessage, "AnotherMessage should not be delivered to the saga after completion");
        }

        public class Context : ScenarioContext
        {
            public Guid Id { get; set; }
            public bool StartSagaMessageReceived { get; set; }
            public bool SagaCompleted { get; set; }
            public bool AnotherMessageReceived { get; set; }
            public bool SagaReceivedAnotherMessage { get; set; }
        }

        public class SagaIsCompletedEndpoint : EndpointConfigurationBuilder
        {
            public SagaIsCompletedEndpoint()
            {
                EndpointSetup<DefaultServer>(b =>
                {
                    b.EnableFeature<TimeoutManager>();
                    b.ExecuteTheseHandlersFirst(typeof(TestSaga12));
                    b.LimitMessageProcessingConcurrencyTo(1); // This test only works if the endpoints processes messages sequentially
                });
            }

            public class TestSaga12 : Saga<TestSagaData12>,
                IAmStartedByMessages<StartSagaMessage>,
                IHandleMessages<CompleteSagaMessage>,
                IHandleMessages<AnotherMessage>
            {
                public TestSaga12(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    testContext.StartSagaMessageReceived = true;
                    return Task.FromResult(0);
                }

                public Task Handle(AnotherMessage message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    testContext.SagaReceivedAnotherMessage = true;
                    return Task.FromResult(0);
                }

                public Task Handle(CompleteSagaMessage message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    MarkAsComplete();
                    testContext.SagaCompleted = true;
                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData12> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                    mapper.ConfigureMapping<CompleteSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                    mapper.ConfigureMapping<AnotherMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                }

                Context testContext;
            }

            public class TestSagaData12 : ContainSagaData
            {
                public virtual Guid SomeId { get; set; }
            }
        }

        public class CompletionHandler : IHandleMessages<AnotherMessage>
        {
            public CompletionHandler(Context testContext)
            {
                this.testContext = testContext;
            }

            public Task Handle(AnotherMessage message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
            {
                testContext.AnotherMessageReceived = true;
                return Task.FromResult(0);
            }

            Context testContext;
        }

        public class StartSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }

        public class CompleteSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }

        public class AnotherMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }
    }
}