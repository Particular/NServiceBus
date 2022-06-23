namespace NServiceBus.AcceptanceTests.Diagnostics
{
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using NServiceBus;

    [NonParallelizable] // Ensure only activities for the current test are captured
    public class When_processing_fails : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_report_failing_message_metrics()
        {
            using var metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();
            _ = await Scenario.Define<Context>()
                .WithEndpoint<FailingEndpoint>(e => e
                    .CustomConfig(cfg => cfg.EnableOpenTelemetryMetrics())
                    .DoNotFailOnErrorMessages()
                    .When(s => s.SendLocal(new FailingMessage())))
                .Done(c => c.HandlerInvoked)
                .Run();

            metricsListener.AssertMetric("messaging.fetches", 1);
            metricsListener.AssertMetric("messaging.failures", 1);
            metricsListener.AssertMetric("messaging.successes", 0);
        }

        //TODO test retries?

        [Test]
        public async Task Should_mark_span_as_failed()
        {
            using var activityListener = TestingActivityListener.SetupNServiceBusDiagnosticListener();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<FailingEndpoint>(e => e
                    .DoNotFailOnErrorMessages()
                    .When(s => s.SendLocal(new FailingMessage())))
                .Done(c => c.HandlerInvoked).Run();

            Assert.AreEqual(1, context.FailedMessages.Count, "the message should have failed");
            Activity failedActivity = activityListener.CompletedActivities.GetIncomingActivities().Single();
            Assert.AreEqual(ActivityStatusCode.Error, failedActivity.Status);
            Assert.AreEqual(ErrorMessage, failedActivity.StatusDescription);

            var tags = failedActivity.Tags.ToImmutableDictionary();
            tags.VerifyTag("otel.status_code", "ERROR");
            tags.VerifyTag("otel.status_description", ErrorMessage);
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
                    throw new SimulatedException(ErrorMessage);
                }
            }
        }

        public class FailingMessage : IMessage
        {
        }

        const string ErrorMessage = "oh no!";
    }
}