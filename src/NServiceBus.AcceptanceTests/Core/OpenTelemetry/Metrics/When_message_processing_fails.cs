namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Metrics;

using System.Collections.Generic;
using System.Threading.Tasks;
using AcceptanceTesting;
using NUnit.Framework;
using AcceptanceTesting.Customization;

public class When_message_processing_fails : OpenTelemetryAcceptanceTest
{
    [Test]
    public async Task Should_report_failing_message_metrics()
    {
        using var metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();
        _ = await Scenario.Define<Context>()
            .WithEndpoint<FailingEndpoint>(e => e
                .DoNotFailOnErrorMessages()
                .CustomConfig(x => x.MakeInstanceUniquelyAddressable("disc"))
                .When(s => s.SendLocal(new FailingMessage())))
            .Run();

        metricsListener.AssertMetric("nservicebus.messaging.fetches", 1);
        metricsListener.AssertMetric("nservicebus.messaging.failures", 1);
        metricsListener.AssertMetric("nservicebus.messaging.successes", 0);
        metricsListener.AssertMetric("nservicebus.messaging.critical_time", 0);
        metricsListener.AssertMetric("nservicebus.messaging.processing_time", 0);
        metricsListener.AssertMetric("nservicebus.messaging.handler_time", 1);

        metricsListener.AssertTags("nservicebus.messaging.fetches",
            new Dictionary<string, object>
            {
                ["nservicebus.queue"] = Conventions.EndpointNamingConvention(typeof(FailingEndpoint)),
                ["nservicebus.discriminator"] = "disc",
                ["nservicebus.message_type"] = typeof(FailingMessage).FullName
            });

        metricsListener.AssertTags("nservicebus.messaging.failures",
            new Dictionary<string, object>
            {
                ["nservicebus.queue"] = Conventions.EndpointNamingConvention(typeof(FailingEndpoint)),
                ["nservicebus.discriminator"] = "disc",
                ["nservicebus.message_type"] = typeof(FailingMessage).FullName,
                ["error.type"] = typeof(SimulatedException).FullName,
            });

        metricsListener.AssertTags("nservicebus.messaging.handler_time",
            new Dictionary<string, object>
            {
                ["nservicebus.queue"] = Conventions.EndpointNamingConvention(typeof(FailingEndpoint)),
                ["nservicebus.discriminator"] = "disc",
                ["nservicebus.message_type"] = typeof(FailingMessage).FullName,
                ["execution.result"] = "failure",
                ["error.type"] = typeof(SimulatedException).FullName,
            });
    }

    class Context : ScenarioContext
    {
    }

    class FailingEndpoint : EndpointConfigurationBuilder
    {
        public FailingEndpoint() => EndpointSetup<OpenTelemetryEnabledEndpoint>();

        class FailingMessageHandler(Context textContext) : IHandleMessages<FailingMessage>
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