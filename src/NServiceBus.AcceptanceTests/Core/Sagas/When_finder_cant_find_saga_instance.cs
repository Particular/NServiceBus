namespace NServiceBus.AcceptanceTests.Core.Sagas;

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

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.FinderUsed, Is.True);
            Assert.That(context.SagaStarted, Is.True);
        }
    }

    public class Context : ScenarioContext
    {
        public bool FinderUsed { get; set; }
        public bool SagaStarted { get; set; }
    }

    public class SagaEndpoint : EndpointConfigurationBuilder
    {
        public SagaEndpoint() =>
            EndpointSetup<DefaultServer>(c => c.UsePersistence<AcceptanceTestingPersistence>());

        class CustomFinder(Context testContext) : ISagaFinder<TestSaga06.SagaData06, StartSagaMessage>
        {
            public Task<TestSaga06.SagaData06> FindBy(StartSagaMessage message, ISynchronizedStorageSession storageSession, IReadOnlyContextBag context, CancellationToken cancellationToken = default)
            {
                testContext.FinderUsed = true;
                return Task.FromResult(default(TestSaga06.SagaData06));
            }
        }

        public class TestSaga06(Context testContext) : Saga<TestSaga06.SagaData06>,
            IAmStartedByMessages<StartSagaMessage>,
            IHandleMessages<SomeOtherMessage>
        {
            public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
            {
                // need to set the correlation property manually because the finder doesn't return an existing instance
                Data.CorrelationProperty = "some value";

                testContext.SagaStarted = true;
                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData06> mapper)
            {
                mapper.ConfigureFinderMapping<StartSagaMessage, CustomFinder>();
                mapper.ConfigureMapping<SomeOtherMessage>(m => m.CorrelationProperty).ToSaga(s => s.CorrelationProperty);
            }

            // This additional, unused, message is required to reproduce https://github.com/Particular/NServiceBus/issues/4888
            public Task Handle(SomeOtherMessage message, IMessageHandlerContext context) => Task.CompletedTask;

            public sealed class SagaData06 : ContainSagaData
            {
                public string CorrelationProperty { get; set; }
            }
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