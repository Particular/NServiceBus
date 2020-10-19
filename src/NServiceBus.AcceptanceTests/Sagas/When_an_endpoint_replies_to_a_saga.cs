namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NUnit.Framework;

    // Repro for issue  https://github.com/NServiceBus/NServiceBus/issues/1277 to test the fix
    // making sure that the saga correlation still works.
    public class When_an_endpoint_replies_to_a_saga : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_correlate_all_saga_messages_properly()
        {
            var context = await Scenario.Define<Context>(c => { c.RunId = Guid.NewGuid(); })
                .WithEndpoint<EndpointThatHostsASaga>(b => b.When((session, ctx) => session.SendLocal(new StartSaga
                {
                    RunId = ctx.RunId
                })))
                .WithEndpoint<EndpointThatRepliesToSagaMessage>()
                .Done(c => c.Done)
                .Run();

            Assert.AreEqual(context.RunId, context.ResponseRunId);
        }

        public class Context : ScenarioContext
        {
            public Guid RunId { get; set; }
            public Guid ResponseRunId { get; set; }
            public bool Done { get; set; }
        }

        public class EndpointThatRepliesToSagaMessage : EndpointConfigurationBuilder
        {
            public EndpointThatRepliesToSagaMessage()
            {
                EndpointSetup<DefaultServer>();
            }

            class DoSomethingHandler : IHandleMessages<DoSomething>
            {
                public Task Handle(DoSomething message, IMessageHandlerContext context)
                {
                    return context.Reply(new DoSomethingResponse
                    {
                        RunId = message.RunId
                    });
                }
            }
        }

        public class EndpointThatHostsASaga : EndpointConfigurationBuilder
        {
            public EndpointThatHostsASaga()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.ConfigureTransport().Routing().RouteToEndpoint(typeof(DoSomething), typeof(EndpointThatRepliesToSagaMessage));
                });
            }

            public class CorrelationTestSaga : Saga<CorrelationTestSaga.CorrelationTestSagaData>,
                IAmStartedByMessages<StartSaga>,
                IHandleMessages<DoSomethingResponse>
            {
                public CorrelationTestSaga(Context context)
                {
                    testContext = context;
                }

                public Task Handle(StartSaga message, IMessageHandlerContext context)
                {
                    return context.Send(new DoSomething
                    {
                        RunId = message.RunId
                    });
                }

                public Task Handle(DoSomethingResponse message, IMessageHandlerContext context)
                {
                    testContext.Done = true;
                    testContext.ResponseRunId = message.RunId;
                    MarkAsComplete();
                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<CorrelationTestSagaData> mapper)
                {
                    mapper.ConfigureMapping<StartSaga>(m => m.RunId).ToSaga(s => s.RunId);
                    mapper.ConfigureMapping<DoSomethingResponse>(m => m.RunId).ToSaga(s => s.RunId);
                }

                public class CorrelationTestSagaData : ContainSagaData
                {
                    public virtual Guid RunId { get; set; }
                }

                Context testContext;
            }
        }

        public class StartSaga : ICommand
        {
            public Guid RunId { get; set; }
        }

        public class DoSomething : ICommand
        {
            public Guid RunId { get; set; }
        }

        public class DoSomethingResponse : IMessage
        {
            public Guid RunId { get; set; }
        }
    }
}