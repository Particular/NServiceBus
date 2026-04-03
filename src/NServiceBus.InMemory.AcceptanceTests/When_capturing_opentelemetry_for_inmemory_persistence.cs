namespace NServiceBus.AcceptanceTests;

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

[NonParallelizable]
public class When_capturing_opentelemetry_for_inmemory_persistence : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_surface_persistence_spans_below_handler_activity()
    {
        using var listener = new AcceptanceActivityListener("NServiceBus.Core", "NServiceBus.InMemory");

        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(builder => builder.When(session => session.SendLocal(new StartSagaMessage { CorrelationId = Guid.NewGuid() })))
            .Done(c => c.SagaCompleted)
            .Run();

        Assert.That(context.SagaCompleted, Is.True);

        var handlerActivities = listener.CompletedActivities.Where(activity => activity.Source.Name == "NServiceBus.Core" && activity.OperationName == "NServiceBus.Diagnostics.InvokeHandler").ToList();
        var saveActivity = listener.CompletedActivities.SingleOrDefault(activity => activity.Source.Name == "NServiceBus.InMemory" && activity.OperationName == "NServiceBus.InMemory.Persistence.Saga.Save");
        var completeActivity = listener.CompletedActivities.SingleOrDefault(activity => activity.Source.Name == "NServiceBus.InMemory" && activity.OperationName == "NServiceBus.InMemory.Persistence.Saga.Complete");

        Assert.Multiple(() =>
        {
            Assert.That(saveActivity, Is.Not.Null);
            Assert.That(completeActivity, Is.Not.Null);
            Assert.That(handlerActivities.Any(handler => saveActivity!.ParentId == handler.Id), Is.True, "expected the saga save span to be parented by a handler activity");
            Assert.That(handlerActivities.Any(handler => completeActivity!.ParentId == handler.Id), Is.True, "expected the saga complete span to be parented by a handler activity");
            Assert.That(saveActivity!.GetTagItem("nservicebus.persistence.storage"), Is.EqualTo("inmemory"));
            Assert.That(completeActivity!.GetTagItem("nservicebus.persistence.operation"), Is.EqualTo("complete"));
        });
    }

    public class Context : ScenarioContext
    {
        public bool SagaCompleted { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() => EndpointSetup<DefaultServer>(config => config.LimitMessageProcessingConcurrencyTo(1));

        [Saga]
        public class TestSaga(Context scenarioContext) : Saga<TestSagaData>,
            IAmStartedByMessages<StartSagaMessage>,
            IHandleMessages<CompleteSagaMessage>
        {
            public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
            {
                Data.CorrelationId = message.CorrelationId;
                return context.SendLocal(new CompleteSagaMessage { CorrelationId = message.CorrelationId });
            }

            public Task Handle(CompleteSagaMessage message, IMessageHandlerContext context)
            {
                scenarioContext.SagaCompleted = true;
                MarkAsComplete();
                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData> mapper) =>
                mapper.MapSaga(s => s.CorrelationId)
                    .ToMessage<StartSagaMessage>(m => m.CorrelationId)
                    .ToMessage<CompleteSagaMessage>(m => m.CorrelationId);
        }

        public class TestSagaData : ContainSagaData
        {
            public Guid CorrelationId { get; set; }
        }
    }

    public class StartSagaMessage : IMessage
    {
        public Guid CorrelationId { get; set; }
    }

    public class CompleteSagaMessage : IMessage
    {
        public Guid CorrelationId { get; set; }
    }

    sealed class AcceptanceActivityListener : IDisposable
    {
        readonly ActivityListener activityListener;
        readonly string[] sourceNames;

        public AcceptanceActivityListener(params string[] sourceNames)
        {
            this.sourceNames = sourceNames;
            activityListener = new ActivityListener
            {
                ShouldListenTo = source => this.sourceNames.Contains(source.Name),
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData
            };
            activityListener.ActivityStopped += activity => CompletedActivities.Enqueue(activity);

            ActivitySource.AddActivityListener(activityListener);
        }

        public ConcurrentQueue<Activity> CompletedActivities { get; } = new();

        public void Dispose() => activityListener.Dispose();
    }
}
