namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Saga;
    using NUnit.Framework;

    [TestFixture]
    public class When_reply_from_a_finder
    {
        [Test]
        public async Task Should_be_received_by_handler()
        {
            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                .WithEndpoint<SagaEndpoint>(b => b.Given((bus, c) =>
                {
                    bus.SendLocal(new StartSagaMessage { Id = c.Id });
                    return Task.FromResult(0);
                }))
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

            class CustomFinder : IFindSagas<TestSagaWithCustomFinder.TestSagaWithCustomFinderSagaData>.Using<StartSagaMessage>
            {
                public IBus Bus { get; set; }
                public Context Context { get; set; }

                public TestSagaWithCustomFinder.TestSagaWithCustomFinderSagaData FindBy(StartSagaMessage message, SagaPersistenceOptions options)
                {
                    Bus.Reply(new SagaNotFoundMessage
                    {
                        Id = Context.Id
                    });
                    return null;
                }
            }

            public class TestSagaWithCustomFinder : Saga<TestSagaWithCustomFinder.TestSagaWithCustomFinderSagaData>,
                IAmStartedByMessages<StartSagaMessage>
            {
                public Context Context { get; set; }

                public void Handle(StartSagaMessage message)
                {
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaWithCustomFinderSagaData> mapper)
                {
                    // not required because of CustomFinder
                }

                public class TestSagaWithCustomFinderSagaData : ContainSagaData
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