namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Metrics;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests.Core.OpenTelemetry;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NServiceBus.Extensibility;
using NServiceBus.Features;
using NUnit.Framework;
using Conventions = AcceptanceTesting.Customization.Conventions;

public class When_envelope_handler_fails : OpenTelemetryAcceptanceTest
{
    [Test]
    public async Task Should_report_failed_unwrapping_metric()
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

        metricsListener.AssertMetric("nservicebus.envelope.unwrapped", 1);

        metricsListener.AssertTags("nservicebus.envelope.unwrapped",
            new Dictionary<string, object>
            {
                ["nservicebus.queue"] = Conventions.EndpointNamingConvention(typeof(EndpointWithMetrics)),
                ["nservicebus.discriminator"] = "discriminator",
                ["nservicebus.envelope.unwrapper_type"] = typeof(ThrowingHandler).FullName,
                ["error.type"] = typeof(InvalidOperationException).FullName
            });
    }

    class ThrowingHandler : IEnvelopeHandler
    {
        public Dictionary<string, string> UnwrapEnvelope(IBufferWriter<byte> bodyWriter, string nativeMessageId, IDictionary<string, string> incomingHeaders, ContextBag extensions, ReadOnlySpan<byte> incomingBody)
            => throw new InvalidOperationException("Some exception");
    }

    class TestEnvelopeFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context) => context.AddEnvelopeHandler<ThrowingHandler>();
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