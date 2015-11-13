namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Features;
    using NServiceBus.Sagas;
    using NUnit.Framework;

    // Repro for issue  https://github.com/NServiceBus/NServiceBus/issues/1277 to test the fix
    // making sure that the saga correlation still works.
    public class When_an_endpoint_replies_to_a_saga : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_correlate_all_saga_messages_properly()
        {
            var context = await Scenario.Define<Context>(c => { c.RunId = Guid.NewGuid(); })
                    .WithEndpoint<EndpointThatHostsASaga>(b => b.When((bus, ctx) => bus.SendLocal(new StartSaga { RunId = ctx.RunId })))
                    .WithEndpoint<EndpointThatRepliesToSagaMessage>()
                    .Done(c => c.Done)
                    .Run();

            Assert.IsTrue(context.DidSagaReplyMessageGetCorrelated);
        }

        public class Context : ScenarioContext
        {
            public Guid RunId { get; set; }
            public bool Done { get; set; }
            public bool DidSagaReplyMessageGetCorrelated { get; set; }
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
                    return context.Reply(new DoSomethingResponse { RunId = message.RunId });
                }
            }
        }

        public class EndpointThatHostsASaga : EndpointConfigurationBuilder
        {
            public EndpointThatHostsASaga()
            {
                EndpointSetup<DefaultServer>(c => c.EnableFeature<TimeoutManager>())
                    .AddMapping<DoSomething>(typeof(EndpointThatRepliesToSagaMessage));

            }

            public class SagaNotFound : IHandleSagaNotFound
            {
                public Context TestContext { get; set; }

                public Task Handle(object message, IMessageProcessingContext context)
                {
                    var lostMessage = message as DoSomethingResponse;
                    if (lostMessage != null && lostMessage.RunId == TestContext.RunId)
                    {
                        TestContext.Done = true;
                    }
                    return Task.FromResult(0);
                }
            }


            public class CorrelationTestSaga : Saga<CorrelationTestSaga.CorrelationTestSagaData>, 
                IAmStartedByMessages<StartSaga>, 
                IHandleMessages<DoSomethingResponse>
            {
                public Context TestContext { get; set; }

                public Task Handle(StartSaga message, IMessageHandlerContext context)
                {
                    return context.Send(new DoSomething { RunId = message.RunId });
                }

                public Task Handle(DoSomethingResponse message, IMessageHandlerContext context)
                {
                    TestContext.Done = true;
                    TestContext.DidSagaReplyMessageGetCorrelated = message.RunId == Data.RunId;
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