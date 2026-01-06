namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.AcceptanceTesting;
using NUnit.Framework;

public class When_ambient_trace_in_message_session : OpenTelemetryAcceptanceTest
{
    [Test]
    public async Task Should_attach_to_ambient_trace()
    {
        using var externalActivitySource = new ActivitySource("external trace source");
        using var _ = TestingActivityListener.SetupDiagnosticListener(externalActivitySource.Name); // need to have a registered listener for activities to be created

        const string wrapperActivityTraceState = "test trace state";

        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithAmbientActivity>(b => b
                .When(async (s, ctx) =>
                {
                    // Otherwise the activity is created with a hierarchical ID format on .NET Framework which resets the RootId once it's converted to W3C format in the send pipeline.
                    var activityTraceContext = new ActivityContext(ActivityTraceId.CreateRandom(),
                        ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded);
                    using var wrapperActivity = externalActivitySource.StartActivity("ambient span", ActivityKind.Server, activityTraceContext);
                    wrapperActivity.TraceStateString = wrapperActivityTraceState;
                    ctx.WrapperActivityId = wrapperActivity.Id;
                    ctx.WrapperActivityRootId = wrapperActivity.RootId;
                    await s.SendLocal(new LocalMessage());
                }))
            .Run();

        var outgoingMessageActivity = NServiceBusActivityListener.CompletedActivities.GetSendMessageActivities().Single();
        var incomingMessageActivity = NServiceBusActivityListener.CompletedActivities.GetReceiveMessageActivities().Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outgoingMessageActivity.ParentId, Is.EqualTo(context.WrapperActivityId), "outgoing message should be connected to the ambient span");
            Assert.That(outgoingMessageActivity.RootId, Is.EqualTo(context.WrapperActivityRootId), "outgoing message should be connected to the ambient trace");
            Assert.That(outgoingMessageActivity.TraceStateString, Is.EqualTo(wrapperActivityTraceState), "ambient trace state should be floated to outgoing message span");
            Assert.That(incomingMessageActivity.ParentId, Is.EqualTo(outgoingMessageActivity.Id), "received message should be connected to send operation span");
            Assert.That(incomingMessageActivity.RootId, Is.EqualTo(context.WrapperActivityRootId), "received message should be connected to the ambient trace");
            Assert.That(incomingMessageActivity.TraceStateString, Is.EqualTo(wrapperActivityTraceState), "ambient trace state should be floated to incoming message span");
        }
    }

    class Context : ScenarioContext
    {
        public string WrapperActivityId { get; set; }
        public string WrapperActivityRootId { get; set; }
    }

    class EndpointWithAmbientActivity : EndpointConfigurationBuilder
    {
        public EndpointWithAmbientActivity() => EndpointSetup<DefaultServer>();

        public class MessageHandler(Context testContext) : IHandleMessages<LocalMessage>
        {
            public Task Handle(LocalMessage message, IMessageHandlerContext context)
            {
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class LocalMessage : IMessage;
}