﻿namespace NServiceBus.AcceptanceTests.Sagas
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Extensibility;
    using Features;
    using NServiceBus;
    using NServiceBus.Sagas;
    using NUnit.Framework;
    using Persistence;

    [TestFixture]
    public class When_a_finder_exists : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_use_it_to_find_saga()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<SagaEndpoint>(b => b.When(session => session.SendLocal(new StartSagaMessage())))
                .Done(c => c.FinderUsed)
                .Run();

            Assert.True(context.FinderUsed);
        }

        public class Context : ScenarioContext
        {
            public bool FinderUsed { get; set; }
        }

        public class SagaEndpoint : EndpointConfigurationBuilder
        {
            public SagaEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.EnableFeature<TimeoutManager>());
            }

            class CustomFinder : IFindSagas<TestSaga06.SagaData06>.Using<StartSagaMessage>
            {
                public Context Context { get; set; }

                public Task<TestSaga06.SagaData06> FindBy(StartSagaMessage message, SynchronizedStorageSession storageSession, ReadOnlyContextBag context)
                {
                    Context.FinderUsed = true;
                    return Task.FromResult(default(TestSaga06.SagaData06));
                }
            }

            public class TestSaga06 : Saga<TestSaga06.SagaData06>, IAmStartedByMessages<StartSagaMessage>
            {
                public Context Context { get; set; }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData06> mapper)
                {
                    // not required because of CustomFinder
                }

                public class SagaData06 : ContainSagaData
                {
                }
            }
        }

        public class StartSagaMessage : IMessage
        {
        }
    }
}