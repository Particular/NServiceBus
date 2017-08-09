namespace NServiceBus.AcceptanceTests.Core.Sagas
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_completing_a_saga : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_hydrate_and_complete_the_existing_instance()
        {
            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                .WithEndpoint<ReceiveCompletesSagaEndpoint>(b =>
                {
                    b.When((session, ctx) => session.SendLocal(new StartSagaMessage
                    {
                        SomeId = ctx.Id
                    }));
                    b.When(ctx => ctx.StartSagaMessageReceived, (session, c) => session.SendLocal(new CompleteSagaMessage
                    {
                        SomeId = c.Id
                    }));
                })
                .Done(c => c.SagaCompleted)
                .Run();

            Assert.True(context.SagaCompleted);
        }

        public class Context : ScenarioContext
        {
            public Guid Id { get; set; }
            public bool StartSagaMessageReceived { get; set; }
            public bool SagaCompleted { get; set; }
        }

        public class ReceiveCompletesSagaEndpoint : EndpointConfigurationBuilder
        {
            public ReceiveCompletesSagaEndpoint()
            {
                EndpointSetup<DefaultServer>(b =>
                {
                    b.ExecuteTheseHandlersFirst(typeof(TestSaga10));
                    b.LimitMessageProcessingConcurrencyTo(1); // This test only works if the endpoints processes messages sequentially
                });
            }

            public class TestSaga10 : Saga<TestSagaData10>,
                IAmStartedByMessages<StartSagaMessage>,
                IHandleMessages<CompleteSagaMessage>
            {
                public Context Context { get; set; }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    Context.StartSagaMessageReceived = true;

                    return Task.FromResult(0);
                }

                public Task Handle(CompleteSagaMessage message, IMessageHandlerContext context)
                {
                    MarkAsComplete();
                    Context.SagaCompleted = true;
                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData10> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                    mapper.ConfigureMapping<CompleteSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                }
            }

            public class TestSagaData10 : ContainSagaData
            {
                public virtual Guid SomeId { get; set; }
            }
        }

        public class StartSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }

        public class CompleteSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }
    }
}