namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Features;
    using NServiceBus.Sagas;
    using NUnit.Framework;

    [TestFixture]
    public class When_reply_from_a_finder
    {
        [Test]
        public async Task Should_be_received_by_handler()
        {
            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                .WithEndpoint<SagaEndpoint>(b => b.When((bus, c) => bus.SendLocalAsync(new StartSagaMessage { Id = c.Id })))
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
                EndpointSetup<DefaultServer>(c => c.EnableFeature<TimeoutManager>());
            }

            class CustomFinder : IFindSagas<TestSagaWithCustomFinder.TestSagaWithCustomFinderSagaData>.Using<StartSagaMessage>
            {
                public IBus Bus { get; set; }
                public Context Context { get; set; }

                public async Task<TestSagaWithCustomFinder.TestSagaWithCustomFinderSagaData> FindBy(StartSagaMessage message, SagaPersistenceOptions options)
                {
                    await Bus.ReplyAsync(new SagaNotFoundMessage
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

                public Task Handle(StartSagaMessage message)
                {
                    return Task.FromResult(0);
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

                public Task Handle(SagaNotFoundMessage message)
                {
                    if (Context.Id != message.Id)
                    {
                        return Task.FromResult(0);
                    }

                    Context.HandlerFired = true;

                    return Task.FromResult(0);
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