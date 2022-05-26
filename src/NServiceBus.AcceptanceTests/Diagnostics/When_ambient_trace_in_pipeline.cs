namespace NServiceBus.AcceptanceTests.Diagnostics
{
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_ambient_trace_in_pipeline : NServiceBusAcceptanceTest
    {
        static ActivitySource externalActivitySource = new ActivitySource("external activity source");

        [Test]
        public async Task Should_attach_to_ambient_trace()
        {
            var activityListener = TestingActivityListener.SetupNServiceBusDiagnosticListener();
            var _ = TestingActivityListener.SetupDiagnosticListener(externalActivitySource.Name); // need to have a registered listener for activities to be created

            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithAmbientActivity>(e => e
                    .When(s => s.SendLocal(new TriggerMessage())))
                .Done(c => c.MessageReceived)
                .Run();

            Assert.AreEqual(context.AmbientActivityId, activityListener.CompletedActivities.Last(a => a.OperationName == "NServiceBus.Diagnostics.OutgoingMessage").ParentId, "the outgoing message should be connected to the ambient trace");
            Assert.AreEqual(activityListener.CompletedActivities.First(a => a.OperationName == "NServiceBus.Diagnostics.IncomingMessage").Id, context.AmbientParentActivityId, "the ambient activity should be connected to the incoming pipeline trace");
        }

        [Test]
        public async Task Should_pass_down_the_trace_state()
        {
            var activityListener = TestingActivityListener.SetupNServiceBusDiagnosticListener();
            _ = TestingActivityListener.SetupDiagnosticListener(externalActivitySource.Name); // need to have a registered listener for activities to be created

            var activityState = "test-state";
            var activityContext = new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.None, activityState);
            _ = externalActivitySource.StartActivity("external", ActivityKind.Server, activityContext);

            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithAmbientActivity>(e => e
                    .When(s => s.SendLocal(new TriggerMessage())))
                .Done(c => c.MessageReceived)
                .Run();

            var outgoingMessageActivities = activityListener.CompletedActivities.FindAll(a => a.OperationName == "NServiceBus.Diagnostics.OutgoingMessage");
            var incomingMessageActivities = activityListener.CompletedActivities.FindAll(a => a.OperationName == "NServiceBus.Diagnostics.IncomingMessage");

            Assert.AreEqual(activityState, context.AmbientActivityState);
            Assert.AreEqual(activityState, outgoingMessageActivities[0].TraceStateString);
            Assert.AreEqual(activityState, outgoingMessageActivities[1].TraceStateString);
            Assert.AreEqual(activityState, incomingMessageActivities[0].TraceStateString);
            Assert.AreEqual(activityState, incomingMessageActivities[1].TraceStateString);
        }

        class Context : ScenarioContext
        {
            public bool MessageReceived { get; set; }
            public string AmbientActivityId { get; set; }
            public string AmbientParentActivityId { get; set; }
            public string AmbientActivityState { get; set; }
        }

        class EndpointWithAmbientActivity : EndpointConfigurationBuilder
        {
            public EndpointWithAmbientActivity() => EndpointSetup<DefaultServer>();

            class MessageHandler : IHandleMessages<TriggerMessage>, IHandleMessages<MessageFromAmbientTrace>
            {
                Context testContext;

                public MessageHandler(Context testContext) => this.testContext = testContext;

                public async Task Handle(TriggerMessage message, IMessageHandlerContext context)
                {
                    using (var ambientActivity = externalActivitySource.StartActivity())
                    {
                        testContext.AmbientActivityId = ambientActivity.Id;
                        testContext.AmbientParentActivityId = ambientActivity.ParentId;
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

        class TriggerMessage : IMessage
        {
        }

        class MessageFromAmbientTrace : IMessage
        {
        }
    }
}