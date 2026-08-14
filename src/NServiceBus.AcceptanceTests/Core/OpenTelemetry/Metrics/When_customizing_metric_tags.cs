namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Metrics;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EndpointTemplates;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.Pipeline;
using NUnit.Framework;
using global::OpenTelemetry;
using global::OpenTelemetry.Metrics;

public class When_customizing_metric_tags : OpenTelemetryAcceptanceTest
{
    const string TotalFetched = "nservicebus.messaging.fetches";
    const string MessageDeserializeTime = "nservicebus.messaging.deserialize_time";
    const string EndpointDiscriminatorTag = "nservicebus.discriminator";
    const string EnclosedMessageTypesTag = "nservicebus.enclosed_message_types";
    const string TenantTag = "acceptance.tenant_id";
    const string FriendlyMessageTypeName = "Order placed (friendly name)";

    [Test]
    public async Task Should_allow_adding_removing_and_overriding_tags_per_instrument()
    {
        using var metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();

        List<Metric> exportedMetrics = [];
        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter("NServiceBus.Core.Pipeline.Incoming")
            .AddView(TotalFetched, new MetricStreamConfiguration
            {
                TagKeys = ["nservicebus.queue", "nservicebus.message_type", TenantTag]
            })
            .AddReader(new BaseExportingMetricReader(new CapturingExporter(exportedMetrics)))
            .Build();

        await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithCustomTags>(b => b.CustomConfig(c => c.MakeInstanceUniquelyAddressable("disc"))
                .When(async session =>
                {
                    var sendOptions = new SendOptions();
                    sendOptions.RouteToThisEndpoint();
                    sendOptions.SetHeader("TenantId", "acme-corp");
                    await session.Send(new MyMessage(), sendOptions);
                }))
            .Run();

        meterProvider.ForceFlush();

        // 1. Add: a tag NServiceBus never defined, sourced from a message header by a custom behavior, shows up on
        // the instrument without any change to NServiceBus itself.
        metricsListener.AssertTags(TotalFetched, new Dictionary<string, object> { [TenantTag] = "acme-corp" });

        // 2. Remove: the raw instrument still carries nservicebus.discriminator, proving NServiceBus itself still
        // reports it...
        metricsListener.AssertTagKeyExists(TotalFetched, EndpointDiscriminatorTag);
        // ...but the view configured above filtered it out of what actually gets exported.
        var exportedFetch = exportedMetrics.Single(m => m.Name == TotalFetched);
        var hasDiscriminatorTag = false;
        foreach (var point in exportedFetch.GetMetricPoints())
        {
            foreach (var tag in point.Tags)
            {
                hasDiscriminatorTag |= tag.Key == EndpointDiscriminatorTag;
            }
        }
        Assert.That(hasDiscriminatorTag, Is.False);

        // 3. Override: enclosed_message_types is overridden to a friendlier value, scoped to just the
        // deserialize_time instrument.
        var overriddenValue = metricsListener.AssertTagKeyExists(MessageDeserializeTime, EnclosedMessageTypesTag);
        Assert.That(overriddenValue, Is.EqualTo(FriendlyMessageTypeName));
    }

    public class Context : ScenarioContext;

    public class EndpointWithCustomTags : EndpointConfigurationBuilder
    {
        public EndpointWithCustomTags() =>
            EndpointSetup<DefaultServer>(c => c.Pipeline.Register(
                new CustomizeMetricTagsBehavior(), "Adds a tenant tag from a header and overrides the enclosed message type tag"));

        [Handler]
        public class MyHandler(Context testContext) : IHandleMessages<MyMessage>
        {
            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    class CustomizeMetricTagsBehavior : Behavior<IIncomingPhysicalMessageContext>
    {
        public override Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
        {
            var tags = context.Extensions.Get<IncomingPipelineMetricTags>();

            if (context.Message.Headers.TryGetValue("TenantId", out var tenantId))
            {
                tags.Add(TenantTag, tenantId, TotalFetched);
            }

            tags.Add(EnclosedMessageTypesTag, FriendlyMessageTypeName, MessageDeserializeTime);

            return next();
        }
    }

    class CapturingExporter(List<Metric> exportedMetrics) : BaseExporter<Metric>
    {
        public override ExportResult Export(in Batch<Metric> batch)
        {
            foreach (var metric in batch)
            {
                exportedMetrics.Add(metric);
            }

            return ExportResult.Success;
        }
    }

    public class MyMessage : IMessage;
}
