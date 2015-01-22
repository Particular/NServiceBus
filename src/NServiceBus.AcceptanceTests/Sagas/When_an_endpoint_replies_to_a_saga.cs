namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using Saga;

    // Repro for issue  https://github.com/NServiceBus/NServiceBus/issues/1277 to test the fix
    // making sure that the saga correlation still works.
    public class When_an_endpoint_replies_to_a_saga : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_correlate_all_saga_messages_properly()
        {
            var context = new Context()
            {
                RunId = Guid.NewGuid()
            };

            Scenario.Define(context)
                    .WithEndpoint<EndpointThatHostsASaga>(b => b.Given((bus, ctx) => bus.SendLocal(new StartSaga { RunId = ctx.RunId })))
                    .WithEndpoint<EndpointThatHandlesAMessageFromSagaAndReplies>()
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

        public class EndpointThatHandlesAMessageFromSagaAndReplies : EndpointConfigurationBuilder
        {
            public EndpointThatHandlesAMessageFromSagaAndReplies()
            {
                EndpointSetup<DefaultServer>();
            }

            class DoSomethingHandler : IHandleMessages<DoSomething>
            {
                public IBus Bus { get; set; }

                public void Handle(DoSomething message)
                {
                    Bus.Reply(new DoSomethingResponse { RunId = message.RunId });
                }
            }
        }

        public class EndpointThatHostsASaga : EndpointConfigurationBuilder
        {
            public EndpointThatHostsASaga()
            {
                EndpointSetup<DefaultServer>()
                    .AddMapping<DoSomething>(typeof (EndpointThatHandlesAMessageFromSagaAndReplies));

            }

            public class SagaNotFound : IHandleSagaNotFound
            {
                public Context Context { get; set; }

                public void Handle(object message)
                {
                    var lostMessage = message as DoSomethingResponse;
                    if (lostMessage != null && lostMessage.RunId == Context.RunId)
                    {
                        Context.Done = true;
                    }
                }
            }


            public class Saga2 : Saga<Saga2.MySaga2Data>, IAmStartedByMessages<StartSaga>, IHandleMessages<DoSomethingResponse>
            {
                public Context Context { get; set; }

                public void Handle(StartSaga message)
                {
                    Data.RunId = message.RunId;
                    Bus.Send(new DoSomething { RunId = message.RunId });
                }

                public void Handle(DoSomethingResponse message)
                {
                    Context.Done = true;
                    Context.DidSagaReplyMessageGetCorrelated = message.RunId == Data.RunId;
                    MarkAsComplete();
                }
                
                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MySaga2Data> mapper)
                {
                    mapper.ConfigureMapping<DoSomethingResponse>(m => m.RunId).ToSaga(s => s.RunId);
                }

                public class MySaga2Data : ContainSagaData
                {
                    [Unique]
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
