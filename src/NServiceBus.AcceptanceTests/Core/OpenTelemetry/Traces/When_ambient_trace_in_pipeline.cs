namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.AcceptanceTesting;
using NUnit.Framework;

public class When_ambient_trace_in_pipeline : OpenTelemetryAcceptanceTest
{
    static ActivitySource externalActivitySource = new(Guid.NewGuid().ToString());

    [Test]
    public async Task Should_attach_to_ambient_trace()
    {
        using var _ = TestingActivityListener.SetupDiagnosticListener(externalActivitySource.Name); // need to have a registered listener for activities to be created

        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithAmbientActivity>(e => e
                .When(s => s.SendLocal(new TriggerMessage())))
            .Done(c => c.MessageReceived)
            .Run();

        var handlerActivity = NServicebusActivityListener.CompletedActivities.GetInvokedHandlerActivities().First();
        var sendFromHandlerActivity = NServicebusActivityListener.CompletedActivities.GetSendMessageActivities().Last();
        Assert.That(sendFromHandlerActivity.ParentId, Is.EqualTo(context.AmbientActivityId), "the outgoing message should be connected to the ambient span");
        Assert.That(sendFromHandlerActivity.RootId, Is.EqualTo(context.AmbientActivityRootId), "outgoing and ambient activity should belong to same trace");
        Assert.That(sendFromHandlerActivity.TraceStateString, Is.EqualTo(ExpectedTraceState), "outgoing activity should capture ambient trace state");
        Assert.That(context.AmbientActivityParentId, Is.EqualTo(handlerActivity.Id), "the ambient activity should be connected to the handler span");
        Assert.That(context.AmbientActivityRootId, Is.EqualTo(handlerActivity.RootId), "handler and ambient activity should belong to same trace");
    }

    class Context : ScenarioContext
    {
        public bool MessageReceived { get; set; }
        public string AmbientActivityId { get; set; }
        public string AmbientActivityParentId { get; set; }
        public string AmbientActivityRootId { get; set; }
        public string AmbientActivityState { get; set; }
    }

    class EndpointWithAmbientActivity : EndpointConfigurationBuilder
    {
        public EndpointWithAmbientActivity() => EndpointSetup<OpenTelemetryEnabledEndpoint>();

        class MessageHandler : IHandleMessages<TriggerMessage>, IHandleMessages<MessageFromAmbientTrace>
        {
            Context testContext;

            public MessageHandler(Context testContext) => this.testContext = testContext;

            public async Task Handle(TriggerMessage message, IMessageHandlerContext context)
            {
                using (var ambientActivity = externalActivitySource.StartActivity())
                {
                    // set/modify trace state:
                    ambientActivity.TraceStateString = ExpectedTraceState;

                    testContext.AmbientActivityId = ambientActivity.Id;
                    testContext.AmbientActivityParentId = ambientActivity.ParentId;
                    testContext.AmbientActivityRootId = ambientActivity.RootId;
                    testContext.AmbientActivityState = ambientActivity.TraceStateString;
                    await context.SendLocal(new MessageFromAmbientTrace());
                }
            }

            public Task Handle(MessageFromAmbientTrace message, IMessageHandlerContext context)
            {
                testContext.MessageReceived = true;
                return Task.CompletedTask;
            }
        }
    }

    public class TriggerMessage : IMessage
    {
    }

    public class MessageFromAmbientTrace : IMessage
    {
    }

    const string ExpectedTraceState = "trace state from ambient activity";
}