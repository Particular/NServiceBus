namespace NServiceBus.AcceptanceTests.Core.Sagas
{
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Extensibility;
    using NServiceBus;
    using NServiceBus.Persistence;
    using NServiceBus.Sagas;
    using NUnit.Framework;

    [TestFixture]
    public class When_finder_cant_find_saga_instance : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_start_new_saga()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<SagaEndpoint>(b => b.When(session => session.SendLocal(new StartSagaMessage())))
                .Done(c => c.SagaStarted)
                .Run();

            Assert.True(context.FinderUsed);
            Assert.True(context.SagaStarted);
        }

        public class Context : ScenarioContext
        {
            public bool FinderUsed { get; set; }
            public bool SagaStarted { get; set; }
        }

        public class SagaEndpoint : EndpointConfigurationBuilder
        {
            public SagaEndpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    //use InMemoryPersistence as custom finder support is required
                    c.UsePersistence<AcceptanceTestingPersistence>();
                });
            }

            class CustomFinder : IFindSagas<TestSaga06.SagaData06>.Using<StartSagaMessage>
            {
                public CustomFinder(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task<TestSaga06.SagaData06> FindBy(StartSagaMessage message, SynchronizedStorageSession storageSession, ReadOnlyContextBag context, CancellationToken cancellationToken = default)
                {
                    testContext.FinderUsed = true;
                    return Task.FromResult(default(TestSaga06.SagaData06));
                }

                Context testContext;
            }

            public class TestSaga06 : Saga<TestSaga06.SagaData06>,
                IAmStartedByMessages<StartSagaMessage>,
                IHandleMessages<SomeOtherMessage>
            {
                public TestSaga06(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    // need to set the correlation property manually because the finder doesn't return an existing instance
                    Data.CorrelationProperty = "some value";

                    testContext.SagaStarted = true;
                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData06> mapper)
                {
                    // no mapping for StartSagaMessage required because of CustomFinder
                    mapper.ConfigureMapping<SomeOtherMessage>(m => m.CorrelationProperty).ToSaga(s => s.CorrelationProperty);
                }

                // This additional, unused, message is required to reprododuce https://github.com/Particular/NServiceBus/issues/4888
                public Task Handle(SomeOtherMessage message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }

                public class SagaData06 : ContainSagaData
                {
                    public virtual string CorrelationProperty { get; set; }
                }

                Context testContext;
            }
        }

        public class StartSagaMessage : IMessage
        {
        }

        public class SomeOtherMessage : IMessage
        {
            public string CorrelationProperty { get; set; }
        }
    }
}