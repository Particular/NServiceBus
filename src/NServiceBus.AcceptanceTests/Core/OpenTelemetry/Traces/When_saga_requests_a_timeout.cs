namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Customization;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NUnit.Framework;

public class When_saga_requests_a_timeout : OpenTelemetryAcceptanceTest
{
    [Test]
    public async Task Should_start_new_trace_on_receive_by_default()
    {
        await Scenario.Define<Context>()
            .WithEndpoint<SagaEndpoint>(b => b
                .When(s => s.SendLocal(new StartSagaMessage { SomeId = Guid.NewGuid().ToString() })))
            .Run();

        var (timeoutSend, timeoutReceive) = GetTimeoutActivities();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(timeoutReceive.TraceId, Is.Not.EqualTo(timeoutSend.TraceId), "a saga timeout should start a new trace on receive by default (backward compatible)");
            Assert.That(timeoutReceive.ParentId, Is.Null, "timeout receive should be a new root");
        }

        var link = timeoutReceive.Links.FirstOrDefault();
        Assert.That(link, Is.Not.Default, "timeout receive should be linked back to the timeout send operation");
        Assert.That(link.Context.TraceId, Is.EqualTo(timeoutSend.TraceId));
    }

    [Test]
    public async Task Should_continue_existing_trace_on_receive_when_configured()
    {
        await Scenario.Define<Context>()
            .WithEndpoint<SagaEndpointContinuingTrace>(b => b
                .When(s => s.SendLocal(new StartSagaMessage { SomeId = Guid.NewGuid().ToString() })))
            .Run();

        var (timeoutSend, timeoutReceive) = GetTimeoutActivities();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(timeoutReceive.TraceId, Is.EqualTo(timeoutSend.TraceId), "a saga timeout should continue the existing trace when SagaTimeoutTraceMode is set to ContinueExisting");
            Assert.That(timeoutReceive.ParentId, Is.EqualTo(timeoutSend.Id));
            Assert.That(timeoutReceive.Links, Is.Empty);
        }
    }

    [Test]
    public async Task Should_not_be_affected_by_send_operation_trace_mode()
    {
        await Scenario.Define<Context>()
            .WithEndpoint<SagaEndpointWithContinuingSendOperationOnly>(b => b
                .When(s => s.SendLocal(new StartSagaMessage { SomeId = Guid.NewGuid().ToString() })))
            .Run();

        var (timeoutSend, timeoutReceive) = GetTimeoutActivities();

        Assert.That(timeoutReceive.TraceId, Is.Not.EqualTo(timeoutSend.TraceId),
            "SendOperationTraceMode must not influence saga timeouts, which are governed independently by SagaTimeoutTraceMode");
    }

    (Activity TimeoutSend, Activity TimeoutReceive) GetTimeoutActivities()
    {
        var sendActivities = NServiceBusActivityListener.CompletedActivities.GetSendMessageActivities();
        var receiveActivities = NServiceBusActivityListener.CompletedActivities.GetReceiveMessageActivities();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sendActivities, Has.Count.EqualTo(2), "start-saga send and timeout send");
            Assert.That(receiveActivities, Has.Count.EqualTo(2), "start-saga receive and timeout receive");
        }

        return (sendActivities[1], receiveActivities[1]);
    }

    public class Context : ScenarioContext
    {
        public bool SagaMarkedComplete { get; set; }
    }

    public class SagaEndpoint : EndpointConfigurationBuilder
    {
        public SagaEndpoint()
        {
            var template = new DefaultServer
            {
                TransportConfiguration = new ConfigureEndpointAcceptanceTestingTransport(false, true)
            };
            EndpointSetup(template, (c, _) => { }, metadata => { });
        }

        [Saga]
        public class TimeoutSaga(Context testContext) : Saga<TimeoutSagaData>, IAmStartedByMessages<StartSagaMessage>, IHandleTimeouts<SagaTimeout>
        {
            public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
            {
                Data.SomeId = message.SomeId;
                return RequestTimeout<SagaTimeout>(context, DateTimeOffset.UtcNow.AddMilliseconds(2));
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TimeoutSagaData> mapper) =>
                mapper.MapSaga(s => s.SomeId).ToMessage<StartSagaMessage>(m => m.SomeId);

            public Task Timeout(SagaTimeout state, IMessageHandlerContext context)
            {
                MarkAsComplete();
                testContext.SagaMarkedComplete = true;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class SagaEndpointContinuingTrace : EndpointConfigurationBuilder
    {
        public SagaEndpointContinuingTrace()
        {
            var template = new DefaultServer
            {
                TransportConfiguration = new ConfigureEndpointAcceptanceTestingTransport(false, true)
            };
            EndpointSetup(template, (c, _) => c.Tracing().DelayedDelivery.SagaTimeoutTraceMode = TraceMode.ContinueExisting, metadata => { });
        }

        [Saga]
        public class TimeoutSaga(Context testContext) : Saga<TimeoutSagaData>, IAmStartedByMessages<StartSagaMessage>, IHandleTimeouts<SagaTimeout>
        {
            public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
            {
                Data.SomeId = message.SomeId;
                return RequestTimeout<SagaTimeout>(context, DateTimeOffset.UtcNow.AddMilliseconds(2));
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TimeoutSagaData> mapper) =>
                mapper.MapSaga(s => s.SomeId).ToMessage<StartSagaMessage>(m => m.SomeId);

            public Task Timeout(SagaTimeout state, IMessageHandlerContext context)
            {
                MarkAsComplete();
                testContext.SagaMarkedComplete = true;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class SagaEndpointWithContinuingSendOperationOnly : EndpointConfigurationBuilder
    {
        public SagaEndpointWithContinuingSendOperationOnly()
        {
            var template = new DefaultServer
            {
                TransportConfiguration = new ConfigureEndpointAcceptanceTestingTransport(false, true)
            };
            EndpointSetup(template, (c, _) => c.Tracing().DelayedDelivery.SendOperationTraceMode = TraceMode.ContinueExisting, metadata => { });
        }

        [Saga]
        public class TimeoutSaga(Context testContext) : Saga<TimeoutSagaData>, IAmStartedByMessages<StartSagaMessage>, IHandleTimeouts<SagaTimeout>
        {
            public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
            {
                Data.SomeId = message.SomeId;
                return RequestTimeout<SagaTimeout>(context, DateTimeOffset.UtcNow.AddMilliseconds(2));
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TimeoutSagaData> mapper) =>
                mapper.MapSaga(s => s.SomeId).ToMessage<StartSagaMessage>(m => m.SomeId);

            public Task Timeout(SagaTimeout state, IMessageHandlerContext context)
            {
                MarkAsComplete();
                testContext.SagaMarkedComplete = true;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class TimeoutSagaData : ContainSagaData
    {
        public virtual string SomeId { get; set; }
    }

    public class StartSagaMessage : IMessage
    {
        public string SomeId { get; set; }
    }

    public class SagaTimeout;
}
