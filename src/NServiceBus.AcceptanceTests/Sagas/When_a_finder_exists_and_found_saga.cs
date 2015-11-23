namespace NServiceBus.AcceptanceTests.Sagas
{
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Extensibility;
    using NServiceBus.Features;
    using NServiceBus.Persistence;
    using NServiceBus.Sagas;
    using NUnit.Framework;

    [TestFixture]
    public class When_a_finder_exists_and_found_saga
    {
        [Test]
        public async Task Should_find_saga_and_not_correlate()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<SagaEndpoint>(b => b.When(bus => bus.SendLocal(new StartSagaMessage())))
                .Done(c => c.Completed)
                .Run();

            Assert.True(context.FinderUsed);
        }

        public class Context : ScenarioContext
        {
            public bool FinderUsed { get; set; }
            public bool Completed { get; set; }
        }

        public class SagaEndpoint : EndpointConfigurationBuilder
        {
            public SagaEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.EnableFeature<TimeoutManager>());
            }

            public class CustomFinder : IFindSagas<TestSaga08.SagaData08>.Using<SomeOtherMessage>
            {
                // ReSharper disable once MemberCanBePrivate.Global
                public Context Context { get; set; }

                public Task<TestSaga08.SagaData08> FindBy(SomeOtherMessage message, SynchronizedStorageSession storageSession, ReadOnlyContextBag context)
                {
                    Context.FinderUsed = true;
                    return Task.FromResult(new TestSaga08.SagaData08
                    {
                        Property = "jfbsjdfbsdjh"
                    });
                }
            }

            public class TestSaga08 : Saga<TestSaga08.SagaData08>,
                IAmStartedByMessages<StartSagaMessage>,
                IHandleMessages<SomeOtherMessage>
            {
                public Context TestContext { get; set; }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    return context.SendLocal(new SomeOtherMessage());
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData08> mapper)
                {
                    // not required because of CustomFinder
                }

                public class SagaData08 : ContainSagaData
                {
                    public virtual string Property { get; set; }
                }

                public Task Handle(SomeOtherMessage message, IMessageHandlerContext context)
                {
                    TestContext.Completed = true;
                    return Task.FromResult(0);
                }
            }

        }

        public class StartSagaMessage : IMessage
        {
        }

        public class SomeOtherMessage : IMessage
        {
        }
    }
}