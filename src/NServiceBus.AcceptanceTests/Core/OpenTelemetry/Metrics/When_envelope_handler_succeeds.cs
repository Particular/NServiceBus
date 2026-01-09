namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Metrics;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests.Core.OpenTelemetry;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NServiceBus.Extensibility;
using NServiceBus.Features;
using NUnit.Framework;
using Conventions = AcceptanceTesting.Customization.Conventions;

public class When_envelope_handler_succeeds : OpenTelemetryAcceptanceTest
{
    [Test]
    public async Task Should_report_successful_unwrapping_metric()
    {
        using var metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();

        _ = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithMetrics>(b => b.CustomConfig(x =>
                {
                    x.MakeInstanceUniquelyAddressable("discriminator");
                    x.EnableFeature<TestEnvelopeFeature>();
                })
                .When(session => session.SendLocal(new OutgoingMessage())))
            .Run();

        // The metric should be explicitly emitted with a value of 0 to indicate no errors occurred
        Assert.That(metricsListener.ReportedMeters["nservicebus.envelope.unwrapped"], Is.EqualTo(0));

        metricsListener.AssertTags("nservicebus.envelope.unwrapped",
            new Dictionary<string, object>
            {
                ["nservicebus.queue"] = Conventions.EndpointNamingConvention(typeof(EndpointWithMetrics)),
                ["nservicebus.discriminator"] = "discriminator",
                ["nservicebus.envelope.unwrapper_type"] = typeof(SuccessfulCloudEventHandler).FullName
            });
    }

    class SuccessfulCloudEventHandler : IEnvelopeHandler
    {
        public Dictionary<string, string> UnwrapEnvelope(string nativeMessageId, IDictionary<string, string> incomingHeaders, ReadOnlySpan<byte> incomingBody, ContextBag extensions, IBufferWriter<byte> bodyWriter)
        {
            bodyWriter.Write(incomingBody);
            return incomingHeaders.ToDictionary();
        }
    }

    class TestEnvelopeFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context) => context.AddEnvelopeHandler<SuccessfulCloudEventHandler>();
    }

    class Context : ScenarioContext;

    class EndpointWithMetrics : EndpointConfigurationBuilder
    {
        public EndpointWithMetrics() => EndpointSetup<DefaultServer>();

        public class MessageHandler(Context testContext) : IHandleMessages<OutgoingMessage>
        {
            public Task Handle(OutgoingMessage message, IMessageHandlerContext context)
            {
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class OutgoingMessage : IMessage;
}