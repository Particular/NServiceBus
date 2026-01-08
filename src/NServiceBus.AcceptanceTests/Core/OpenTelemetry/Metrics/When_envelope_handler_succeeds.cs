namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Metrics;

using System;
using System.Collections.Generic;
using System.Linq;
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
                .When(async (session, ctx) =>
                {
                    await session.SendLocal(new OutgoingMessage());
                }))
            .Done(c => c.OutgoingMessagesReceived == 1)
            .Run();

        // The metric should be explicitly emitted with a value of 0 to indicate no errors occurred
        Assert.That(metricsListener.ReportedMeters["nservicebus.envelope.unwrapping_error"], Is.EqualTo(0));

        metricsListener.AssertTags("nservicebus.envelope.unwrapping_error",
            new Dictionary<string, object>
            {
                ["nservicebus.queue"] = Conventions.EndpointNamingConvention(typeof(EndpointWithMetrics)),
                ["nservicebus.discriminator"] = "discriminator",
                ["nservicebus.envelope.unwrapper_type"] = typeof(SuccessfulCloudEventHandler).FullName
            });
    }

    class SuccessfulCloudEventHandler : IEnvelopeHandler
    {
        public (Dictionary<string, string> headers, ReadOnlyMemory<byte> body)? UnwrapEnvelope(string nativeMessageId, IDictionary<string, string> incomingHeaders,
            ContextBag extensions, ReadOnlyMemory<byte> incomingBody) =>
            (incomingHeaders.ToDictionary(), incomingBody);
    }

    class TestEnvelopeFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context) => context.AddEnvelopeHandler<SuccessfulCloudEventHandler>();
    }

    class Context : ScenarioContext
    {
        public int OutgoingMessagesReceived;
    }

    class EndpointWithMetrics : EndpointConfigurationBuilder
    {
        public EndpointWithMetrics() => EndpointSetup<DefaultServer>();

        public class MessageHandler(Context testContext) : IHandleMessages<OutgoingMessage>
        {
            public Task Handle(OutgoingMessage message, IMessageHandlerContext context)
            {
                Interlocked.Increment(ref testContext.OutgoingMessagesReceived);
                return Task.CompletedTask;
            }
        }
    }

    public class OutgoingMessage : IMessage
    {
    }
}