namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using EndpointTemplates;
using NServiceBus.AcceptanceTesting;
using NUnit.Framework;

public class When_retrying_messages : OpenTelemetryAcceptanceTest
{
    [Test]
    public async Task Should_correlate_immediate_retry_with_send()
    {
        await Scenario.Define<Context>()
            .WithEndpoint<RetryingEndpoint>(e => e
                .CustomConfig(c => c.Recoverability().Immediate(i => i.NumberOfRetries(1)))
                .DoNotFailOnErrorMessages()
                .When(s => s.SendLocal(new FailingMessage())))
            .Run();

        var receiveActivities = NServiceBusActivityListener.CompletedActivities.GetReceiveMessageActivities();
        var sendActivities = NServiceBusActivityListener.CompletedActivities.GetSendMessageActivities();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sendActivities, Has.Count.EqualTo(1));
            Assert.That(receiveActivities, Has.Count.EqualTo(2), "the message should be processed twice due to one immediate retry");
        }
        using (Assert.EnterMultipleScope())
        {
            Assert.That(receiveActivities[0].ParentId, Is.EqualTo(sendActivities[0].Id), "should not change parent span");
            Assert.That(receiveActivities[1].ParentId, Is.EqualTo(sendActivities[0].Id), "should not change parent span");

            Assert.That(sendActivities.Concat(receiveActivities).All(a => a.TraceId == sendActivities[0].TraceId), Is.True, "all activities should be part of the same trace");
        }
    }

    [Test]
    public async Task Should_start_new_trace_on_receive_for_delayed_retry_by_default()
    {
        await Scenario.Define<Context>()
            .WithEndpoint<RetryingEndpoint>(e => e
                .CustomConfig(c => c.Recoverability().Delayed(i => i.NumberOfRetries(1).TimeIncrease(TimeSpan.FromMilliseconds(1))))
                .DoNotFailOnErrorMessages()
                .When(s => s.SendLocal(new FailingMessage())))
            .Run();

        var (sendRequest, firstAttempt, retryAttempt) = GetDelayedRetryActivities();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(firstAttempt.TraceId, Is.EqualTo(sendRequest.TraceId), "the first attempt is part of the original send's trace");
            Assert.That(firstAttempt.ParentId, Is.EqualTo(sendRequest.Id));

            Assert.That(retryAttempt.TraceId, Is.Not.EqualTo(sendRequest.TraceId), "a delayed retry should start a new trace on receive by default (backward compatible)");
            Assert.That(retryAttempt.ParentId, Is.Null, "the retry attempt should be a new root");
        }

        var link = retryAttempt.Links.FirstOrDefault();
        Assert.That(link, Is.Not.Default, "the retry attempt should be linked back to the original send operation");
        Assert.That(link.Context.TraceId, Is.EqualTo(sendRequest.TraceId));
    }

    [Test]
    public async Task Should_continue_existing_trace_on_receive_for_delayed_retry_when_configured()
    {
        await Scenario.Define<Context>()
            .WithEndpoint<RetryingEndpoint>(e => e
                .CustomConfig(c =>
                {
                    c.Recoverability().Delayed(i => i.NumberOfRetries(1).TimeIncrease(TimeSpan.FromMilliseconds(1)));
                    c.Tracing().Recoverability.DelayedRetryTraceMode = TraceMode.ContinueExisting;
                })
                .DoNotFailOnErrorMessages()
                .When(s => s.SendLocal(new FailingMessage())))
            .Run();

        var (sendRequest, _, retryAttempt) = GetDelayedRetryActivities();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(retryAttempt.TraceId, Is.EqualTo(sendRequest.TraceId), "a delayed retry should continue the existing trace when DelayedRetryTraceMode is set to ContinueExisting");
            Assert.That(retryAttempt.ParentId, Is.EqualTo(sendRequest.Id));
            Assert.That(retryAttempt.Links, Is.Empty);
        }
    }

    (Activity SendRequest, Activity FirstAttempt, Activity RetryAttempt) GetDelayedRetryActivities()
    {
        var receiveActivities = NServiceBusActivityListener.CompletedActivities.GetReceiveMessageActivities();
        var sendActivities = NServiceBusActivityListener.CompletedActivities.GetSendMessageActivities();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sendActivities, Has.Count.EqualTo(1));
            Assert.That(receiveActivities, Has.Count.EqualTo(2), "the message should be processed twice due to one delayed retry");
        }

        return (sendActivities[0], receiveActivities[0], receiveActivities[1]);
    }

    public class Context : ScenarioContext
    {
        public int InvocationCounter { get; set; }
    }

    public class RetryingEndpoint : EndpointConfigurationBuilder
    {
        public RetryingEndpoint()
        {
            var template = new DefaultServer
            {
                TransportConfiguration = new ConfigureEndpointAcceptanceTestingTransport(false, true)
            };
            EndpointSetup(template, (c, _) => { }, metadata => { });
        }

        [Handler]
        public class Handler(Context testContext) : IHandleMessages<FailingMessage>
        {
            public Task Handle(FailingMessage message, IMessageHandlerContext context)
            {
                testContext.InvocationCounter++;

                if (testContext.InvocationCounter == 1)
                {
                    throw new SimulatedException("first attempt fails");
                }

                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }
    
    public class FailingMessage : IMessage;
}