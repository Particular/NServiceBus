namespace NServiceBus.AcceptanceTests.Core.Sagas;

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
            .Run();

        Assert.That(context.FinderUsed, Is.True);
    }

    public class Context : ScenarioContext
    {
        public bool FinderUsed { get; set; }
    }

    public class SagaEndpoint : EndpointConfigurationBuilder
    {
        public SagaEndpoint() => EndpointSetup<DefaultServer>();

        public class CustomFinder(Context testContext, ISagaPersister sagaPersister) : ISagaFinder<TestSaga08.SagaData08, SomeOtherMessage>
        {
            public async Task<TestSaga08.SagaData08> FindBy(SomeOtherMessage message, ISynchronizedStorageSession storageSession, IReadOnlyContextBag context, CancellationToken cancellationToken = default)
            {
                testContext.FinderUsed = true;
                var sagaData = await sagaPersister.Get<TestSaga08.SagaData08>(message.SagaId, storageSession, (ContextBag)context, cancellationToken).ConfigureAwait(false);
                return sagaData;
            }
        }

        public class TestSaga08(Context testContext) : Saga<TestSaga08.SagaData08>,
            IAmStartedByMessages<StartSagaMessage>,
            IHandleMessages<SomeOtherMessage>
        {
            public Task Handle(StartSagaMessage message, IMessageHandlerContext context) =>
                context.SendLocal(new SomeOtherMessage
                {
                    SagaId = Data.Id
                });

            public Task Handle(SomeOtherMessage message, IMessageHandlerContext context)
            {
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData08> mapper)
            {
                mapper.MapSaga(s => s.Property)
                    .ToMessage<StartSagaMessage>(m => m.Property);
                mapper.ConfigureFinderMapping<SomeOtherMessage, CustomFinder>();
            }

            public class SagaData08 : ContainSagaData
            {
                public virtual string Property { get; set; }
            }
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