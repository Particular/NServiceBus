namespace NServiceBus.AcceptanceTests.Sagas
{
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
            var context = Scenario.Define<Context>()
                .WithEndpoint<SagaEndpoint>(b => b.Given(bus => bus.SendLocal(new StartSagaMessage())))
                .Done(c => c.HandlerFired)
                .Run();

            Assert.True(context.HandlerFired);
        }

        public class Context : ScenarioContext
        {
            public bool HandlerFired { get; set; }
        }

        public class SagaEndpoint : EndpointConfigurationBuilder
        {
            public SagaEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            class CustomFinder : IFindSagas<TestSaga.SagaData>.Using<StartSagaMessage>
            {
                public Context Context { get; set; }
                public IBus Bus { get; set; }

                public ISagaPersister Persister { get; set; }

                public TestSaga.SagaData FindBy(StartSagaMessage message)
                {
                    Bus.Reply(new SagaNotFoundMessage());
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
                    Context.HandlerFired = true;
                }
            }

        }
        public class SagaNotFoundMessage : IMessage
        {
        }

        public class StartSagaMessage : IMessage
        {
        }
    }
}