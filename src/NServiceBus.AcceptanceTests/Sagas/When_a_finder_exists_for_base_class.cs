namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Extensibility;
    using Features;
    using NServiceBus.Sagas;
    using NUnit.Framework;
    using Persistence;

    [TestFixture]
    public class When_a_finder_exists_for_base_class : NServiceBusAcceptanceTest
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

            class CustomFinder : IFindSagas<FinderForBaseClassSaga.FinderForBaseClassSagaData>.Using<StartSagaMessageBase>
            {
                public Context Context { get; set; }

                public Task<FinderForBaseClassSaga.FinderForBaseClassSagaData> FindBy(StartSagaMessageBase message, SynchronizedStorageSession storageSession, ReadOnlyContextBag context)
                {
                    Context.FinderUsed = true;
                    return Task.FromResult(default(FinderForBaseClassSaga.FinderForBaseClassSagaData));
                }
            }

            public class FinderForBaseClassSaga : Saga<FinderForBaseClassSaga.FinderForBaseClassSagaData>, IAmStartedByMessages<StartSagaMessage>
            {
                public Context Context { get; set; }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<FinderForBaseClassSagaData> mapper)
                {
                    // not required because of CustomFinder
                }

                public class FinderForBaseClassSagaData : ContainSagaData
                {
                }
            }
        }

        public class StartSagaMessage : StartSagaMessageBase
        {
        }

        public class StartSagaMessageBase : IMessage
        {
            public Guid SomeId { get; set; }
        }
    }
}