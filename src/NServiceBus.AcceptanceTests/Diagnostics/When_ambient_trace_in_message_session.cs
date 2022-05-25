namespace NServiceBus.AcceptanceTests.Diagnostics
{
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_ambient_trace_in_message_session : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_attach_to_ambient_trace()
        {
            var externalActivitySource = new ActivitySource("external trace source");
            var activityListener = TestingActivityListener.SetupNServiceBusDiagnosticListener();
            var _ = TestingActivityListener.SetupDiagnosticListener(externalActivitySource.Name); // need to have a registered listener for activities to be created
            string wrapperActivityRootId = null;

            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithAmbientActivity>(b => b
                    .When(async s =>
                    {
                        using (var wrapperActivity = externalActivitySource.StartActivity())
                        {
                            wrapperActivityRootId = wrapperActivity.RootId;
                            await s.SendLocal(new LocalMessage());
                        }
                    }))
                .Done(c => c.OutgoingMessageReceived)
                .Run();

            var outgoingMessageActivities = activityListener.CompletedActivities.FindAll(a => a.OperationName == "NServiceBus.Diagnostics.OutgoingMessage");
            var incomingMessageActivities = activityListener.CompletedActivities.FindAll(a => a.OperationName == "NServiceBus.Diagnostics.IncomingMessage");

            Assert.AreEqual(wrapperActivityRootId, outgoingMessageActivities.Single().RootId);
            Assert.AreEqual(wrapperActivityRootId, incomingMessageActivities.Single().RootId);
        }

        class Context : ScenarioContext
        {
            public bool OutgoingMessageReceived { get; set; }
        }

        class EndpointWithAmbientActivity : EndpointConfigurationBuilder
        {
            public EndpointWithAmbientActivity() => EndpointSetup<DefaultServer>();

            public class MessageHandler : IHandleMessages<LocalMessage>
            {
                Context testContext;

                public MessageHandler(Context testContext) => this.testContext = testContext;

                public Task Handle(LocalMessage message, IMessageHandlerContext context)
                {
                    testContext.OutgoingMessageReceived = true;
                    return Task.CompletedTask;
                }
            }
        }

        class LocalMessage : IMessage
        {
        }
    }
}