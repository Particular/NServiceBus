namespace NServiceBus.AcceptanceTests.Core.Sagas
{
    using System;
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
    public class When_finder_returns_existing_saga : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_use_existing_saga()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<SagaEndpoint>(b => b
                    .When(session => session.SendLocal(new StartSagaMessage
                    {
                        Property = "Test"
                    })))
                .Done(c => c.HandledOtherMessage)
                .Run();

            Assert.True(context.FinderUsed);
        }

        public class Context : ScenarioContext
        {
            public bool FinderUsed { get; set; }
            public bool HandledOtherMessage { get; set; }
        }

        public class SagaEndpoint : EndpointConfigurationBuilder
        {
            public SagaEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class CustomFinder : IFindSagas<TestSaga08.SagaData08>.Using<SomeOtherMessage>
            {
                public CustomFinder(Context testContext, ISagaPersister sagaPersister)
                {
                    this.testContext = testContext;
                    this.sagaPersister = sagaPersister;
                }

                public async Task<TestSaga08.SagaData08> FindBy(SomeOtherMessage message, SynchronizedStorageSession storageSession, ReadOnlyContextBag context, CancellationToken cancellationToken = default)
                {
                    testContext.FinderUsed = true;
                    var sagaData = await sagaPersister.Get<TestSaga08.SagaData08>(message.SagaId, storageSession, (ContextBag)context, cancellationToken).ConfigureAwait(false);
                    return sagaData;
                }

                Context testContext;
                ISagaPersister sagaPersister;
            }

            public class TestSaga08 : Saga<TestSaga08.SagaData08>,
                IAmStartedByMessages<StartSagaMessage>,
                IHandleMessages<SomeOtherMessage>
            {
                public TestSaga08(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    return context.SendLocal(new SomeOtherMessage
                    {
                        SagaId = Data.Id
                    });
                }

                public Task Handle(SomeOtherMessage message, IMessageHandlerContext context)
                {
                    testContext.HandledOtherMessage = true;
                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData08> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(saga => saga.Property).ToSaga(saga => saga.Property);
                    // Mapping not required for SomeOtherMessage because CustomFinder used
                }

                public class SagaData08 : ContainSagaData
                {
                    public virtual string Property { get; set; }
                }

                Context testContext;
            }
        }

        public class StartSagaMessage : IMessage
        {
            public string Property { get; set; }
        }

        public class SomeOtherMessage : IMessage
        {
            public Guid SagaId { get; set; }
        }
    }
}
