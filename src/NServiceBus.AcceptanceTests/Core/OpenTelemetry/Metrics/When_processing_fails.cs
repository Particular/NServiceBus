namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Metrics;

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NUnit.Framework;

public class When_processing_fails : OpenTelemetryAcceptanceTest
{
    [Test]
    public async Task Should_report_failing_message_metrics()
    {
        using var metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();
        _ = await Scenario.Define<Context>()
            .WithEndpoint<FailingEndpoint>(e => e
                .DoNotFailOnErrorMessages()
                .When(s => s.SendLocal(new FailingMessage())))
            .Done(c => c.HandlerInvoked)
            .Run();

        metricsListener.AssertMetric("nservicebus.messaging.fetches", 1);
        metricsListener.AssertMetric("nservicebus.messaging.failures", 1);
        metricsListener.AssertMetric("nservicebus.messaging.successes", 0);
    }

    class Context : ScenarioContext
    {
        public bool HandlerInvoked { get; set; }
    }

    class FailingEndpoint : EndpointConfigurationBuilder
    {
        public FailingEndpoint() => EndpointSetup<OpenTelemetryEnabledEndpoint>();

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