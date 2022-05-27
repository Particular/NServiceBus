namespace NServiceBus.AcceptanceTests.Diagnostics
{
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_processing_fails : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_mark_span_as_failed()
        {
            var activityListener = TestingActivityListener.SetupNServiceBusDiagnosticListener();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<FailingEndpoint>(e => e
                    .DoNotFailOnErrorMessages()
                    .When(s => s.SendLocal(new FailingMessage())))
                .Done(c => c.HandlerInvoked).Run();

            Assert.AreEqual(1, context.FailedMessages.Count, "the message should have failed");
            Assert.AreEqual(ActivityStatusCode.Error, activityListener.CompletedActivities.GetIncomingActivities().Single().Status);
        }

        class Context : ScenarioContext
        {
            public bool HandlerInvoked { get; set; }
        }

        class FailingEndpoint : EndpointConfigurationBuilder
        {
            public FailingEndpoint() => EndpointSetup<DefaultServer>();

            class FailingMessageHandler : IHandleMessages<FailingMessage>
            {
                Context textContext;

                public FailingMessageHandler(Context textContext) => this.textContext = textContext;

                public Task Handle(FailingMessage message, IMessageHandlerContext context)
                {
                    textContext.HandlerInvoked = true;
                    throw new SimulatedException();
                }
            }
        }

        class FailingMessage : IMessage
        {
        }
    }
}