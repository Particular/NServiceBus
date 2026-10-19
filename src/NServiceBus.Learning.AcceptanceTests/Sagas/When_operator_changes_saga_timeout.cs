namespace NServiceBus.AcceptanceTests.Sagas;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Pipeline;
using NServiceBus.Routing;
using NServiceBus.Transport;
using NUnit.Framework;

public class When_operator_changes_saga_timeout : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_apply_the_change_through_the_management_satellite()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<ServiceControlSpy>()
            .WithEndpoint<Endpoint>(e => e.When(s => s.SendLocal(new StartSaga { Id = Guid.NewGuid().ToString() })))
            .Done(c => c.FiredPayload is not null && ExpectedReports.All(t => c.Received(t).Length > 0))
            .Run();

        var rescheduled = context.Received("SagaTimeoutRescheduled").Single();
        var triggered = context.Received("SagaTimeoutTriggered").Single();
        var canceled = context.Received("SagaTimeoutCanceled").Single();
        var rejected = context.Received("SagaTimeoutChangeRejected").Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.FiredPayload, Is.EqualTo("set by operator"));

            Assert.That(rescheduled.GetProperty("OperationId").GetGuid(), Is.EqualTo(context.RescheduleOperationId));
            Assert.That(rescheduled.GetProperty("Version").GetInt32(), Is.EqualTo(2));
            Assert.That(triggered.GetProperty("TriggerId").GetString(), Is.EqualTo(rescheduled.GetProperty("TriggerId").GetString()));

            Assert.That(rejected.GetProperty("OperationId").GetGuid(), Is.EqualTo(context.StaleCancelOperationId));
            Assert.That(rejected.GetProperty("Reason").GetString(), Is.EqualTo("VersionMismatch"));
            Assert.That(rejected.GetProperty("RequestedChange").GetString(), Is.EqualTo("Cancel"));

            Assert.That(canceled.GetProperty("OperationId").GetGuid(), Is.EqualTo(context.CancelOperationId));
            Assert.That(canceled.GetProperty("Version").GetInt32(), Is.EqualTo(1));
        }
    }

    static readonly string[] ExpectedReports = ["SagaTimeoutRescheduled", "SagaTimeoutTriggered", "SagaTimeoutCanceled", "SagaTimeoutChangeRejected"];

    public class Context : ScenarioContext
    {
        public Guid RescheduleOperationId { get; } = Guid.NewGuid();
        public Guid StaleCancelOperationId { get; } = Guid.NewGuid();
        public Guid CancelOperationId { get; } = Guid.NewGuid();
        public string FiredPayload { get; set; }
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
        });

        [Saga]
        public class OperatorManagedSaga(Context testContext) : Saga<OperatorManagedSaga.SagaData>,
            IAmStartedByMessages<StartSaga>,
            IHandleTimeouts<RescheduledTimeout>,
            IHandleTimeouts<CanceledTimeout>
        {
            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
                mapper.MapSaga(s => s.CorrelationProperty).ToMessage<StartSaga>(m => m.Id);

            public async Task Handle(StartSaga message, IMessageHandlerContext context)
            {
                await RequestTimeout(context, TimeSpan.FromHours(1), new RescheduledTimeout { Value = "set by saga" });
                await RequestTimeout<CanceledTimeout>(context, TimeSpan.FromHours(1));
            }

            public Task Timeout(RescheduledTimeout state, IMessageHandlerContext context)
            {
                testContext.FiredPayload = state.Value;
                return Task.CompletedTask;
            }

            public Task Timeout(CanceledTimeout state, IMessageHandlerContext context) => Task.CompletedTask;

            public class SagaData : ContainSagaData
            {
                public virtual string CorrelationProperty { get; set; }
            }
        }
    }

    // Plays the part of ServiceControl: records reports and requests changes through the reported management address
    public class ServiceControlSpy : EndpointConfigurationBuilder
    {
        public ServiceControlSpy() => EndpointSetup<DefaultServer>(c =>
            c.Pipeline.Register<RecordPlatformMessagesAndRequestChanges>("Records the raw platform messages and requests timeout changes"));

        class RecordPlatformMessagesAndRequestChanges(Context testContext) : Behavior<IIncomingPhysicalMessageContext>
        {
            public override async Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
            {
                var messageType = context.Message.Headers[Headers.EnclosedMessageTypes].Split('.').Last();
                using var document = JsonDocument.Parse(context.Message.Body);
                var body = document.RootElement.Clone();
                testContext.PlatformMessages.Enqueue((messageType, body));

                var dispatcher = context.Builder.GetRequiredService<IMessageDispatcher>();

                if (messageType == "SagaTimeoutRequested")
                {
                    if (body.GetProperty("TimeoutType").GetString()!.EndsWith(nameof(RescheduledTimeout)))
                    {
                        var payload = Encoding.UTF8.GetBytes("""{"Value":"set by operator"}""");
                        await Request(dispatcher, body, testContext.RescheduleOperationId, "Reschedule", 1, DateTimeOffset.UtcNow, payload);
                    }
                    else
                    {
                        await Request(dispatcher, body, testContext.StaleCancelOperationId, "Cancel", 7);
                    }
                }

                // Requested after the rejection, so the stale request can't find the timeout already canceled
                if (messageType == "SagaTimeoutChangeRejected")
                {
                    await Request(dispatcher, body, testContext.CancelOperationId, "Cancel", 1);
                }
            }

            static Task Request(IMessageDispatcher dispatcher, JsonElement requested, Guid operationId, string change, int expectedVersion, DateTimeOffset? dueTime = null, byte[] payload = null)
            {
                var headers = new Dictionary<string, string>
                {
                    ["NServiceBus.SagaTimeoutManagement.OperationId"] = operationId.ToString(),
                    ["NServiceBus.SagaTimeoutManagement.SagaType"] = requested.GetProperty("SagaType").GetString(),
                    ["NServiceBus.SagaTimeoutManagement.SagaId"] = requested.GetProperty("SagaId").GetString(),
                    ["NServiceBus.SagaTimeoutManagement.TimeoutId"] = requested.GetProperty("TimeoutId").GetString(),
                    ["NServiceBus.SagaTimeoutManagement.ExpectedVersion"] = expectedVersion.ToString(),
                    ["NServiceBus.SagaTimeoutManagement.Change"] = change
                };

                if (dueTime is not null)
                {
                    headers["NServiceBus.SagaTimeoutManagement.DueTime"] = DateTimeOffsetHelper.ToWireFormattedString(dueTime.Value);
                }

                if (payload is not null)
                {
                    headers[Headers.ContentType] = "application/json";
                }

                var request = new OutgoingMessage(Guid.NewGuid().ToString(), headers, payload ?? []);
                var destination = new UnicastAddressTag(requested.GetProperty("ManagementAddress").GetString());
                return dispatcher.Dispatch(new TransportOperations(new TransportOperation(request, destination)), new TransportTransaction());
            }
        }
    }

    public class StartSaga : ICommand
    {
        public string Id { get; set; }
    }

    public class RescheduledTimeout
    {
        public string Value { get; set; }
    }

    public class CanceledTimeout;
}
