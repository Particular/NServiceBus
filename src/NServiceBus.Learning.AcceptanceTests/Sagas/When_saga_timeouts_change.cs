namespace NServiceBus.AcceptanceTests.Sagas;

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using NServiceBus.Pipeline;
using NUnit.Framework;

public class When_saga_timeouts_change : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_report_each_committed_change_to_ServiceControl()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<ServiceControlSpy>()
            .WithEndpoint<Endpoint>(e => e.When(s => s.SendLocal(new StartSaga { Id = Guid.NewGuid().ToString() })))
            .Done(c => c.Received("SagaCompleted").Length > 0)
            .Run();

        var requested = context.Received("SagaTimeoutRequested");
        var rescheduled = context.Received("SagaTimeoutRescheduled").Single();
        var canceled = context.Received("SagaTimeoutCanceled").Single();
        var rejected = context.Received("SagaTimeoutChangeRejected").Single();
        var triggered = context.Received("SagaTimeoutTriggered").Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(requested, Has.Length.EqualTo(3));
            Assert.That(requested.Select(r => r.GetProperty("Version").GetInt32()), Is.All.EqualTo(1));
            Assert.That(requested.Select(r => r.GetProperty("CorrelationPropertyValue").GetString()), Is.All.EqualTo(context.SagaCorrelationId));

            // The first attempt to change the timeouts failed, so only the committed retry is reported
            Assert.That(rescheduled.GetProperty("TimeoutId").GetString(), Is.EqualTo(context.FiredTimeoutId));
            Assert.That(rescheduled.GetProperty("Version").GetInt32(), Is.EqualTo(2));
            Assert.That(canceled.GetProperty("TimeoutId").GetString(), Is.EqualTo(context.CanceledTimeoutId));
            Assert.That(rejected.GetProperty("TimeoutId").GetString(), Is.EqualTo("unknown-timeout"));
            Assert.That(rejected.GetProperty("RequestedChange").GetString(), Is.EqualTo("Cancel"));

            Assert.That(triggered.GetProperty("TimeoutId").GetString(), Is.EqualTo(context.FiredTimeoutId));
            Assert.That(triggered.GetProperty("Version").GetInt32(), Is.EqualTo(2));
            Assert.That(triggered.GetProperty("TriggerId").GetString(), Is.EqualTo(rescheduled.GetProperty("TriggerId").GetString()));

            Assert.That(context.PlatformMessages.Select(m => m.Body.GetProperty("ManagementAddress").GetString()), Is.All.Not.Empty);
            Assert.That(context.PlatformMessages.Select(m => m.Body.GetProperty("HostId").GetString()), Is.All.Not.Empty);
            Assert.That(rescheduled.GetProperty("OperationId").ValueKind, Is.EqualTo(JsonValueKind.Null));
        }
    }

    [Test]
    public async Task Should_report_completion_after_the_last_timeout_is_canceled()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<ServiceControlSpy>()
            .WithEndpoint<Endpoint>(e => e.When(s => s.SendLocal(new StartSagaToCancel { Id = Guid.NewGuid().ToString() })))
            .Done(c => c.Received("SagaCompleted").Length > 0)
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Received("SagaTimeoutRequested"), Has.Length.EqualTo(1));
            Assert.That(context.Received("SagaCompleted").Single().GetProperty("CorrelationPropertyValue").GetString(), Is.EqualTo(context.SagaCorrelationId));
            // A completing saga reports only the completion, which discards all its timeouts
            Assert.That(context.Received("SagaTimeoutCanceled"), Is.Empty);
        }
    }

    public class Context : ScenarioContext
    {
        public string SagaCorrelationId { get; set; }
        public string FiredTimeoutId { get; set; }
        public string CanceledTimeoutId { get; set; }
        public bool ChangeAttemptFailed { get; set; }
        public ConcurrentQueue<(string MessageType, JsonElement Body)> PlatformMessages { get; } = new();

        public JsonElement[] Received(string messageType) =>
            [.. PlatformMessages.Where(m => m.MessageType == messageType).Select(m => m.Body)];
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() => EndpointSetup<DefaultServer>(c =>
        {
            c.ServiceControlErrorInstanceQueue(Conventions.EndpointNamingConvention(typeof(ServiceControlSpy)));
            c.ReportSagaTimeoutsToServiceControl();
            _ = c.Recoverability().Immediate(i => i.NumberOfRetries(1));
        });

        [Saga]
        public class ReportingTimeoutsSaga(Context testContext) : Saga<ReportingTimeoutsSaga.SagaData>,
            IAmStartedByMessages<StartSaga>,
            IHandleMessages<ChangeTimeouts>,
            IHandleTimeouts<FiredTimeout>,
            IHandleTimeouts<CanceledTimeout>,
            IHandleTimeouts<PendingTimeout>
        {
            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
                mapper.MapSaga(s => s.CorrelationProperty)
                    .ToMessage<StartSaga>(m => m.Id)
                    .ToMessage<ChangeTimeouts>(m => m.Id);

            public async Task Handle(StartSaga message, IMessageHandlerContext context)
            {
                testContext.SagaCorrelationId = message.Id;
                await RequestTimeout<FiredTimeout>(context, TimeSpan.FromHours(1));
                await RequestTimeout<CanceledTimeout>(context, TimeSpan.FromHours(1));
                await RequestTimeout<PendingTimeout>(context, TimeSpan.FromHours(1));
                await context.SendLocal(new ChangeTimeouts { Id = message.Id });
            }

            public Task Handle(ChangeTimeouts message, IMessageHandlerContext context)
            {
                var fired = FindTimeouts<FiredTimeout>(_ => true).Single();
                var canceled = FindTimeouts<CanceledTimeout>(_ => true).Single();
                testContext.FiredTimeoutId = fired.TimeoutId;
                testContext.CanceledTimeoutId = canceled.TimeoutId;

                RescheduleTimeout(fired.TimeoutId, DateTimeOffset.UtcNow.AddSeconds(1));
                CancelTimeout(canceled.TimeoutId);
                CancelTimeout("unknown-timeout");

                if (!testContext.ChangeAttemptFailed)
                {
                    testContext.ChangeAttemptFailed = true;
                    throw new SimulatedException("Roll back the first attempt");
                }

                return Task.CompletedTask;
            }

            public Task Timeout(FiredTimeout state, IMessageHandlerContext context)
            {
                // PendingTimeout is still stored, so completing reports SagaCompleted
                MarkAsComplete();
                return Task.CompletedTask;
            }

            public Task Timeout(CanceledTimeout state, IMessageHandlerContext context) => Task.CompletedTask;

            public Task Timeout(PendingTimeout state, IMessageHandlerContext context) => Task.CompletedTask;

            public class SagaData : ContainSagaData
            {
                public virtual string CorrelationProperty { get; set; }
            }
        }

        [Saga]
        public class CancelAndCompleteSaga(Context testContext) : Saga<CancelAndCompleteSaga.SagaData>,
            IAmStartedByMessages<StartSagaToCancel>,
            IHandleMessages<CancelAndComplete>,
            IHandleTimeouts<CanceledTimeout>
        {
            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
                mapper.MapSaga(s => s.CorrelationProperty)
                    .ToMessage<StartSagaToCancel>(m => m.Id)
                    .ToMessage<CancelAndComplete>(m => m.Id);

            public async Task Handle(StartSagaToCancel message, IMessageHandlerContext context)
            {
                testContext.SagaCorrelationId = message.Id;
                await RequestTimeout<CanceledTimeout>(context, TimeSpan.FromHours(1));
                await context.SendLocal(new CancelAndComplete { Id = message.Id });
            }

            public Task Handle(CancelAndComplete message, IMessageHandlerContext context)
            {
                CancelTimeout(FindTimeouts<CanceledTimeout>(_ => true).Single().TimeoutId);
                MarkAsComplete();
                return Task.CompletedTask;
            }

            public Task Timeout(CanceledTimeout state, IMessageHandlerContext context) => Task.CompletedTask;

            public class SagaData : ContainSagaData
            {
                public virtual string CorrelationProperty { get; set; }
            }
        }
    }

    public class ServiceControlSpy : EndpointConfigurationBuilder
    {
        public ServiceControlSpy() => EndpointSetup<DefaultServer>(c =>
            c.Pipeline.Register<RecordPlatformMessages>("Records the raw platform messages instead of handling them"));

        class RecordPlatformMessages(Context testContext) : Behavior<IIncomingPhysicalMessageContext>
        {
            public override Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
            {
                var messageType = context.Message.Headers[Headers.EnclosedMessageTypes].Split('.').Last();
                using var body = JsonDocument.Parse(context.Message.Body);
                testContext.PlatformMessages.Enqueue((messageType, body.RootElement.Clone()));
                return Task.CompletedTask;
            }
        }
    }

    public class StartSaga : ICommand
    {
        public string Id { get; set; }
    }

    public class ChangeTimeouts : ICommand
    {
        public string Id { get; set; }
    }

    public class StartSagaToCancel : ICommand
    {
        public string Id { get; set; }
    }

    public class CancelAndComplete : ICommand
    {
        public string Id { get; set; }
    }

    public class FiredTimeout;

    public class CanceledTimeout;

    public class PendingTimeout;
}
