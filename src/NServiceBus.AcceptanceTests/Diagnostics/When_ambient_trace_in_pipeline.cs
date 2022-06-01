namespace NServiceBus.AcceptanceTests.Diagnostics
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    [NonParallelizable] // Ensure only activities for the current test are captured
    public class When_ambient_trace_in_pipeline : NServiceBusAcceptanceTest
    {
        static ActivitySource externalActivitySource = new(Guid.NewGuid().ToString());

        [Test]
        public async Task Should_attach_to_ambient_trace()
        {
            using var activityListener = TestingActivityListener.SetupNServiceBusDiagnosticListener();
            using var _ = TestingActivityListener.SetupDiagnosticListener(externalActivitySource.Name); // need to have a registered listener for activities to be created

            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithAmbientActivity>(e => e
                    .When(s => s.SendLocal(new TriggerMessage())))
                .Done(c => c.MessageReceived)
                .Run();

            var incomingMessageActiviy = activityListener.CompletedActivities.GetIncomingActivities().First();
            var sendFromHandlerActivity = activityListener.CompletedActivities.GetOutgoingActivities().Last();
            Assert.AreEqual(context.AmbientActivityId, sendFromHandlerActivity.ParentId, "the outgoing message should be connected to the ambient span");
            Assert.AreEqual(context.AmbientActivityRootId, sendFromHandlerActivity.RootId, "outgoing and ambient activity should belong to same trace");
            Assert.AreEqual(ExpectedTraceState, sendFromHandlerActivity.TraceStateString, "outgoing activity should capture ambient trace state");
            Assert.AreEqual(incomingMessageActiviy.Id, context.AmbientActivityParentId, "the ambient activity should be connected to the incoming pipeline span");
            Assert.AreEqual(incomingMessageActiviy.RootId, context.AmbientActivityRootId, "incoming and ambient activity should belong to same trace");
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
            public EndpointWithAmbientActivity() => EndpointSetup<DefaultServer>();

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
}