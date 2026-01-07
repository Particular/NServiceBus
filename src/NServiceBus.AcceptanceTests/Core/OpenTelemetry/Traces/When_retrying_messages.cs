namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System;
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
    public async Task Should_correlate_delayed_retry_with_send()
    {
        Requires.DelayedDelivery();

        await Scenario.Define<Context>()
            .WithEndpoint<RetryingEndpoint>(e => e
                .CustomConfig(c => c.Recoverability().Delayed(i => i.NumberOfRetries(1).TimeIncrease(TimeSpan.FromMilliseconds(1))))
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

    class Context : ScenarioContext
    {
        public int InvocationCounter { get; set; }
    }

    class RetryingEndpoint : EndpointConfigurationBuilder
    {
        public RetryingEndpoint() => EndpointSetup<DefaultServer>();

        class Handler(Context testContext) : IHandleMessages<FailingMessage>
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

    public class FailingMessage : IMessage
    {
    }
}