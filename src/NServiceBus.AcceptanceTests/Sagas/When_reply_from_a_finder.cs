namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Saga;
    using NUnit.Framework;

    [TestFixture]
    public class When_reply_from_a_finder
    {
        [Test]
        public void Should_be_received_by_handler()
        {
            var context = new Context
            {
                Id = Guid.NewGuid()
            };

            Scenario.Define(context)
                .WithEndpoint<SagaEndpoint>(b => b.Given(bus => bus.SendLocal(new StartSagaMessage
                                                                              {
                                                                                  Id = context.Id
                                                                              })))
                .Done(c => c.HandlerFired)
                .Run();

            Assert.True(context.HandlerFired);
        }

        public class Context : ScenarioContext
        {
            public bool HandlerFired { get; set; }
            public Guid Id { get; set; }
        }

        public class SagaEndpoint : EndpointConfigurationBuilder
        {
            public SagaEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            class CustomFinder : IFindSagas<TestSaga.SagaData>.Using<StartSagaMessage>
            {
                public IBus Bus { get; set; }
                public Context Context { get; set; }

                public TestSaga.SagaData FindBy(StartSagaMessage message)
                {
                    Bus.Reply(new SagaNotFoundMessage
                              {
                                  Id = Context.Id
                              });
                    return null;
                }
            }

            public class TestSaga : Saga<TestSaga.SagaData>,
                IAmStartedByMessages<StartSagaMessage>
            {
                public Context Context { get; set; }

                public void Handle(StartSagaMessage message)
                {
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
                {
                }

                public class SagaData : ContainSagaData
                {
                }
            }

            public class Handler : IHandleMessages<SagaNotFoundMessage>
            {
                public Context Context { get; set; }

                public void Handle(SagaNotFoundMessage message)
                {
                    if (Context.Id != message.Id)
                    {
                        return;
                    }
                    Context.HandlerFired = true;
                }
            }
        }

        public class SagaNotFoundMessage : IMessage
        {
            public Guid Id { get; set; }
        }

        public class StartSagaMessage : IMessage
        {
            public Guid Id { get; set; }
        }
    }
}