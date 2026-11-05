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
    // Remove in v11 together with the V11BehaviorSwitch block in NServiceBus.Core/obsoletes-v10.cs.
    [Test]
    public async Task Should_report_failed_unwrapping_metric()
    {
        using var metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();

        await ReceiveEnvelopedMessage();

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

    [Test]
    [OpenTelemetryV11Defaults]
    public async Task Should_count_the_unwrap_attempt_with_error_type()
    {
        using var metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();

        await ReceiveEnvelopedMessage();

        metricsListener.AssertMetric("nservicebus.envelope.unwrap_attempts", 1);
        metricsListener.AssertTags("nservicebus.envelope.unwrap_attempts",
            new Dictionary<string, object>
            {
                ["nservicebus.queue"] = Conventions.EndpointNamingConvention(typeof(EndpointWithMetrics)),
                ["nservicebus.discriminator"] = "discriminator",
                ["nservicebus.envelope.unwrapper_type"] = typeof(ThrowingHandler).FullName,
                ["error.type"] = typeof(InvalidOperationException).FullName
            });
    }

    static Task<Context> ReceiveEnvelopedMessage() => Scenario.Define<Context>()
        .WithEndpoint<EndpointWithMetrics>(b => b.CustomConfig(x =>
            {
                x.MakeInstanceUniquelyAddressable("discriminator");
                x.EnableFeature<TestEnvelopeFeature>();
            })
            .When(session => session.SendLocal(new OutgoingMessage())))
        .Run();

    class ThrowingHandler : IEnvelopeHandler
    {
        public Dictionary<string, string> UnwrapEnvelope(string nativeMessageId, IDictionary<string, string> incomingHeaders, ReadOnlySpan<byte> incomingBody, ContextBag extensions, IBufferWriter<byte> bodyWriter)
            => throw new InvalidOperationException("Some exception");
    }

    class TestEnvelopeFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context) => context.AddEnvelopeHandler<ThrowingHandler>();
    }

    public class Context : ScenarioContext;

    public class EndpointWithMetrics : EndpointConfigurationBuilder
    {
        public EndpointWithMetrics() => EndpointSetup<DefaultServer>();

        [Handler]
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