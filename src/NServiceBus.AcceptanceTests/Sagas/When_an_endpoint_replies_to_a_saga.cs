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
                    .WithEndpoint<EndpointThatHostsASaga>(b => b.When((bus, ctx) => bus.SendLocalAsync(new StartSaga { RunId = ctx.RunId })))
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
                public IBus Bus { get; set; }

                public Task Handle(DoSomething message)
                {
                    return Bus.ReplyAsync(new DoSomethingResponse { RunId = message.RunId });
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
                public Context Context { get; set; }

                public Task Handle(object message)
                {
                    var lostMessage = message as DoSomethingResponse;
                    if (lostMessage != null && lostMessage.RunId == Context.RunId)
                    {
                        Context.Done = true;
                    }
                    return Task.FromResult(0);
                }
            }


            public class CorrelationTestSaga : Saga<CorrelationTestSaga.CorrelationTestSagaData>, IAmStartedByMessages<StartSaga>, IHandleMessages<DoSomethingResponse>
            {
                public Context Context { get; set; }

                public Task Handle(StartSaga message)
                {
                    Data.RunId = message.RunId;
                    return Bus.SendAsync(new DoSomething { RunId = message.RunId });
                }

                public Task Handle(DoSomethingResponse message)
                {
                    Context.Done = true;
                    Context.DidSagaReplyMessageGetCorrelated = message.RunId == Data.RunId;
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


        [Serializable]
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
