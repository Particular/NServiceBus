namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry;

using System;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NUnit.Framework;

public class When_retrying_messages : OpenTelemetryAcceptanceTest
{
    [Test]
    public async Task Should_correlate_immediate_retry_with_send()
    {
        using var activityListener = TestingActivityListener.SetupNServiceBusDiagnosticListener();

        await Scenario.Define<Context>()
            .WithEndpoint<RetryingEndpoint>(e => e
                .CustomConfig(c => c.Recoverability().Immediate(i => i.NumberOfRetries(1)))
                .DoNotFailOnErrorMessages()
                .When(s => s.SendLocal(new FailingMessage())))
            .Done(c => c.InvocationCounter == 2)
            .Run();

        var receiveActivities = activityListener.CompletedActivities.GetIncomingActivities();
        var sendActivities = activityListener.CompletedActivities.GetOutgoingActivities();

        Assert.AreEqual(1, sendActivities.Count);
        Assert.AreEqual(2, receiveActivities.Count, "the message should be processed twice due to one immediate retry");
        Assert.AreEqual(sendActivities[0].Id, receiveActivities[0].ParentId, "should not change parent span");
        Assert.AreEqual(sendActivities[0].Id, receiveActivities[1].ParentId, "should not change parent span");

        Assert.IsTrue(sendActivities.Concat(receiveActivities).All(a => a.TraceId == sendActivities[0].TraceId), "all activities should be part of the same trace");
    }

    [Test]
    public async Task Should_correlate_delayed_retry_with_send()
    {
        Requires.DelayedDelivery();

        using var activityListener = TestingActivityListener.SetupNServiceBusDiagnosticListener();

        await Scenario.Define<Context>()
            .WithEndpoint<RetryingEndpoint>(e => e
                .CustomConfig(c => c.Recoverability().Delayed(i => i.NumberOfRetries(1).TimeIncrease(TimeSpan.FromMilliseconds(1))))
                .DoNotFailOnErrorMessages()
                .When(s => s.SendLocal(new FailingMessage())))
            .Done(c => c.InvocationCounter == 2)
            .Run();

        var receiveActivities = activityListener.CompletedActivities.GetIncomingActivities();
        var sendActivities = activityListener.CompletedActivities.GetOutgoingActivities();

        Assert.AreEqual(1, sendActivities.Count);
        Assert.AreEqual(2, receiveActivities.Count, "the message should be processed twice due to one immediate retry");
        Assert.AreEqual(sendActivities[0].Id, receiveActivities[0].ParentId, "should not change parent span");
        Assert.AreEqual(sendActivities[0].Id, receiveActivities[1].ParentId, "should not change parent span");

        Assert.IsTrue(sendActivities.Concat(receiveActivities).All(a => a.TraceId == sendActivities[0].TraceId), "all activities should be part of the same trace");
    }

    class Context : ScenarioContext
    {
        public int InvocationCounter { get; set; }
    }

    class RetryingEndpoint : EndpointConfigurationBuilder
    {
        public RetryingEndpoint()
        {
            EndpointSetup<DefaultServer>();
        }

        class Handler : IHandleMessages<FailingMessage>
        {
            Context testContext;

            public Handler(Context testContext)
            {
                this.testContext = testContext;
            }

            public Task Handle(FailingMessage message, IMessageHandlerContext context)
            {
                testContext.InvocationCounter++;

                if (testContext.InvocationCounter == 1)
                {
                    throw new SimulatedException("first attempt fails");
                }

                return Task.CompletedTask;
            }
        }
    }

    public class FailingMessage : IMessage
    {
    }
}