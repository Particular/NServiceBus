namespace NServiceBus.AcceptanceTests.Core.Sagas;

using System;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Extensibility;
using NServiceBus.Persistence;
using NServiceBus.Sagas;
using NUnit.Framework;

[TestFixture]
public class When_no_finder_support : NServiceBusAcceptanceTest
{
    [Test]
    public void Should_throw()
    {
        var exception = Assert.ThrowsAsync<Exception>(async () => await Scenario.Define<Context>()
            .WithEndpoint<SagaEndpoint>(b => b.When(session => session.SendLocal(new StartSagaMessage())))
            .Done(c => c.EndpointsStarted)
            .Run());

        Assert.That(exception.Message, Does.StartWith("The selected persistence doesn't support custom sagas finders"));
    }

    public class Context : ScenarioContext;

    public class SagaEndpoint : EndpointConfigurationBuilder
    {
        public SagaEndpoint() =>
            EndpointSetup<DefaultServer>(c => c.UsePersistence<AcceptanceTestingPersistenceWithoutFinderSupport>());

        class CustomFinder : ISagaFinder<TestSaga09.SagaData09, StartSagaMessage>
        {
            public Task<TestSaga09.SagaData09> FindBy(StartSagaMessage message, ISynchronizedStorageSession storageSession, IReadOnlyContextBag context, CancellationToken cancellationToken = default) => Task.FromResult(default(TestSaga09.SagaData09));
        }

        public class TestSaga09 : Saga<TestSaga09.SagaData09>,
            IAmStartedByMessages<StartSagaMessage>
        {
            public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
            {
                Data.CorrelationProperty = "some value";

                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData09> mapper) => mapper.ConfigureFinderMapping<StartSagaMessage, CustomFinder>();

            public class SagaData09 : ContainSagaData
            {
                public virtual string CorrelationProperty { get; set; }
            }
        }
    }

    public class StartSagaMessage : IMessage;
}