using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NUnit.Framework;

namespace NServiceBus.AcceptanceTests.Diagnostics;

[NonParallelizable] // Ensure only activities for the current test are captured
public class When_retrying_messages : NServiceBusAcceptanceTest
{
    //TODO immediate retried
    //TODO delayed retries

    [Test]
    public async Task Should_correlate_immediate_retry_with_send()
    {
        using var activityListener = TestingActivityListener.SetupNServiceBusDiagnosticListener();

        await Scenario.Define<Context>()
            .WithEndpoint<RetryingEndpoint>(e => e
                .CustomConfig(c => c.Recoverability().Immediate(i => i.NumberOfRetries(1)))
                .DoNotFailOnErrorMessages()
                .When(s => s.SendLocal(new FailingMessage())))
            .Done(c => c.RetryActivity != null)
            .Run();

        var receiveActivities = activityListener.CompletedActivities.GetIncomingActivities();
        var sendActivities = activityListener.CompletedActivities.GetOutgoingActivities();

        Assert.AreEqual(1, sendActivities.Count);
        Assert.AreEqual(2, receiveActivities.Count, "the message should be processed twice due to one immediate retry");
        Assert.AreEqual(sendActivities[0].Id, receiveActivities[0].ParentId, "should not change parent span");
        Assert.AreEqual(sendActivities[0].Id, receiveActivities[1].ParentId, "should not change parent span");

        Assert.IsTrue(activityListener.CompletedActivities.All(a => a.TraceId == sendActivities[0].TraceId), "all activities should be part of the same trace");
    }

    [Test]
    public void Should_correlate_delayed_retry_with_send()
    {
        
    }

    class Context : ScenarioContext
    {
        public int InvocationCounter { get; set; }
        public Activity FirstReceiveActivity { get; set; }
        public Activity RetryActivity { get; set; }
    }

    class RetryingEndpoint : EndpointConfigurationBuilder
    {
        public RetryingEndpoint()
        {
            EndpointSetup<DefaultServer>();
        }

        class Handler : IHandleMessages<FailingMessage>
        {
            private Context testContext;

            public Handler(Context testContext)
            {
                this.testContext = testContext;
            }

            public Task Handle(FailingMessage message, IMessageHandlerContext context)
            {
                testContext.InvocationCounter++;

                if (testContext.InvocationCounter == 1)
                {
                    testContext.FirstReceiveActivity = Activity.Current;
                    throw new SimulatedException("first attempt fails");
                }

                testContext.RetryActivity = Activity.Current;

                return Task.CompletedTask;
            }
        }
    }

    class FailingMessage : IMessage
    {
    }
}