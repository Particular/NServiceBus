namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Metrics;

using System.Collections.Generic;
using System.Threading.Tasks;
using AcceptanceTesting;
using NUnit.Framework;
using AcceptanceTesting.Customization;
using EndpointTemplates;

public class When_message_processing_fails : OpenTelemetryAcceptanceTest
{
    const string FetchesMetric = "nservicebus.messaging.fetches";
    const string FailuresMetric = "nservicebus.messaging.failures";
    const string ProcessingAttemptsMetric = "nservicebus.messaging.processing_attempts";
    const string HandlerTimeMetric = "nservicebus.messaging.handler_time";
    const string QueueTag = "nservicebus.queue";
    const string DiscriminatorTag = "nservicebus.discriminator";
    const string MessageTypeTag = "nservicebus.message_type";
    const string ErrorTypeTag = "error.type";

    // Remove in v11 together with the V11BehaviorSwitch block in NServiceBus.Core/obsoletes-v10.cs.
    [Test]
    public async Task Should_report_failing_message_metrics()
    {
        using var metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();
        await SendFailingMessage();

        metricsListener.AssertMetric(FetchesMetric, 1);
        metricsListener.AssertMetric(FailuresMetric, 1);
        metricsListener.AssertMetric("nservicebus.messaging.successes", 0);
        metricsListener.AssertMetric("nservicebus.messaging.critical_time", 0);
        metricsListener.AssertMetric("nservicebus.messaging.processing_time", 0);
        metricsListener.AssertMetric(HandlerTimeMetric, 1);

        metricsListener.AssertTags(FetchesMetric,
            new Dictionary<string, object>
            {
                [QueueTag] = Conventions.EndpointNamingConvention(typeof(FailingEndpoint)),
                [DiscriminatorTag] = "disc",
                [MessageTypeTag] = typeof(FailingMessage).FullName
            });

        metricsListener.AssertTags(FailuresMetric,
            new Dictionary<string, object>
            {
                [QueueTag] = Conventions.EndpointNamingConvention(typeof(FailingEndpoint)),
                [DiscriminatorTag] = "disc",
                [MessageTypeTag] = typeof(FailingMessage).FullName,
                [ErrorTypeTag] = typeof(SimulatedException).FullName,
            });

        metricsListener.AssertTags(HandlerTimeMetric,
            new Dictionary<string, object>
            {
                [QueueTag] = Conventions.EndpointNamingConvention(typeof(FailingEndpoint)),
                [DiscriminatorTag] = "disc",
                [MessageTypeTag] = typeof(FailingMessage).FullName,
                ["execution.result"] = "failure",
                [ErrorTypeTag] = typeof(SimulatedException).FullName
            });
    }

    [Test]
    [OpenTelemetryV11Defaults]
    public async Task Should_report_a_processing_attempt_with_error_type()
    {
        using var metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();
        await SendFailingMessage();

        metricsListener.AssertMetric(ProcessingAttemptsMetric, 1);
        metricsListener.AssertTags(ProcessingAttemptsMetric,
            new Dictionary<string, object>
            {
                [QueueTag] = Conventions.EndpointNamingConvention(typeof(FailingEndpoint)),
                [DiscriminatorTag] = "disc",
                [MessageTypeTag] = typeof(FailingMessage).FullName,
                [ErrorTypeTag] = typeof(SimulatedException).FullName,
            });
    }

    static Task<Context> SendFailingMessage() => Scenario.Define<Context>()
        .WithEndpoint<FailingEndpoint>(e => e
            .DoNotFailOnErrorMessages()
            .CustomConfig(x => x.MakeInstanceUniquelyAddressable("disc"))
            .When(s => s.SendLocal(new FailingMessage())))
        .Run();

    public class Context : ScenarioContext
    {
    }

    public class FailingEndpoint : EndpointConfigurationBuilder
    {
        public FailingEndpoint() => EndpointSetup<DefaultServer>();

        [Handler]
        public class FailingMessageHandler(Context textContext) : IHandleMessages<FailingMessage>
        {
            public Task Handle(FailingMessage message, IMessageHandlerContext context)
            {
                textContext.MarkAsCompleted();
                throw new SimulatedException(ErrorMessage);
            }

            const string ErrorMessage = "oh no!";
        }
    }

    public class FailingMessage : IMessage;
}