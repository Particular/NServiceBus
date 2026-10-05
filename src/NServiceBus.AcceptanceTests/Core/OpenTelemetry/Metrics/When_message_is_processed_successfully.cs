namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Metrics;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EndpointTemplates;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NUnit.Framework;
using Conventions = AcceptanceTesting.Customization.Conventions;

public class When_message_is_processed_successfully : OpenTelemetryAcceptanceTest
{
    const string SuccessesMetric = "nservicebus.messaging.successes";
    const string FetchesMetric = "nservicebus.messaging.fetches";
    const string CriticalTimeMetric = "nservicebus.messaging.critical_time";
    const string ProcessingTimeMetric = "nservicebus.messaging.processing_time";
    const string HandlerTimeMetric = "nservicebus.messaging.handler_time";
    const string QueueTag = "nservicebus.queue";
    const string DiscriminatorTag = "nservicebus.discriminator";
    const string MessageTypeTag = "nservicebus.message_type";

    [Test]
    public async Task Should_report_successful_message_metric()
    {
        using var metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();

        _ = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithMetrics>(b => b.CustomConfig(x => x.MakeInstanceUniquelyAddressable("disc"))
                .When(async (session, ctx) =>
                {
                    for (var x = 0; x < 5; x++)
                    {
                        await session.SendLocal(new OutgoingMessage());
                    }
                }))
            .Run();

        metricsListener.AssertMetric(SuccessesMetric, 5);
        metricsListener.AssertMetric(FetchesMetric, 5);
        metricsListener.AssertMetric("nservicebus.messaging.failures", 0);
        metricsListener.AssertMetric(CriticalTimeMetric, 5);
        metricsListener.AssertMetric(ProcessingTimeMetric, 5);
        metricsListener.AssertMetric(HandlerTimeMetric, 5);

        metricsListener.AssertTags(FetchesMetric,
            new Dictionary<string, object>
            {
                [QueueTag] = Conventions.EndpointNamingConvention(typeof(EndpointWithMetrics)),
                [DiscriminatorTag] = "disc",
                [MessageTypeTag] = typeof(OutgoingMessage).FullName
            });

        metricsListener.AssertTags(SuccessesMetric,
            new Dictionary<string, object>
            {
                [QueueTag] = Conventions.EndpointNamingConvention(typeof(EndpointWithMetrics)),
                [DiscriminatorTag] = "disc",
                [MessageTypeTag] = typeof(OutgoingMessage).FullName
            });

        metricsListener.AssertTags(CriticalTimeMetric,
            new Dictionary<string, object>
            {
                [QueueTag] = Conventions.EndpointNamingConvention(typeof(EndpointWithMetrics)),
                [DiscriminatorTag] = "disc",
                [MessageTypeTag] = typeof(OutgoingMessage).FullName
            });

        metricsListener.AssertTags(ProcessingTimeMetric,
            new Dictionary<string, object>
            {
                [QueueTag] = Conventions.EndpointNamingConvention(typeof(EndpointWithMetrics)),
                [DiscriminatorTag] = "disc",
                [MessageTypeTag] = typeof(OutgoingMessage).FullName
            });

        metricsListener.AssertTags(HandlerTimeMetric,
            new Dictionary<string, object>
            {
                [QueueTag] = Conventions.EndpointNamingConvention(typeof(EndpointWithMetrics)),
                [DiscriminatorTag] = "disc",
                [MessageTypeTag] = typeof(OutgoingMessage).FullName,
                ["nservicebus.message_handler_type"] = typeof(EndpointWithMetrics.MessageHandler).FullName,
                ["execution.result"] = "success"
            });
    }

    [Test]
    public async Task Should_only_tag_most_concrete_type_on_metric()
    {
        using var metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();

        _ = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithMetrics>(b => b
                .When(async (session, ctx) =>
                {
                    for (var x = 0; x < 5; x++)
                    {
                        await session.SendLocal(new OutgoingWithComplexHierarchyMessage());
                    }
                }))
            .Run();

        metricsListener.AssertMetric(SuccessesMetric, 5);
        metricsListener.AssertMetric(FetchesMetric, 5);
        metricsListener.AssertMetric("nservicebus.messaging.failures", 0);

        var successEndpoint =
            metricsListener.AssertTagKeyExists(SuccessesMetric, QueueTag);
        var successType =
            metricsListener.AssertTagKeyExists(SuccessesMetric, MessageTypeTag);
        var successHandlerType =
            metricsListener.AssertTagKeyExists(SuccessesMetric, "nservicebus.message_handler_types");

        var fetchedEndpoint = metricsListener.AssertTagKeyExists(FetchesMetric, QueueTag);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(successEndpoint, Is.EqualTo(Conventions.EndpointNamingConvention(typeof(EndpointWithMetrics))));
            Assert.That(fetchedEndpoint, Is.EqualTo(Conventions.EndpointNamingConvention(typeof(EndpointWithMetrics))));
            Assert.That(successType, Is.EqualTo(typeof(OutgoingWithComplexHierarchyMessage).FullName));
            Assert.That(successHandlerType, Is.EqualTo(typeof(EndpointWithMetrics.ComplexMessageHandler).FullName));
        }
    }

    public class Context : ScenarioContext
    {
        public int OutgoingMessagesReceived;
        public int ComplexOutgoingMessagesReceived;
    }

    public class EndpointWithMetrics : EndpointConfigurationBuilder
    {
        public EndpointWithMetrics() => EndpointSetup<DefaultServer>();

        [Handler]
        public class MessageHandler(Context testContext) : IHandleMessages<OutgoingMessage>
        {
            public Task Handle(OutgoingMessage message, IMessageHandlerContext context)
            {
                var numberOfMessages = Interlocked.Increment(ref testContext.OutgoingMessagesReceived);
                testContext.MarkAsCompleted(numberOfMessages == 5);
                return Task.CompletedTask;
            }
        }

        [Handler]
        public class ComplexMessageHandler(Context testContext) : IHandleMessages<OutgoingWithComplexHierarchyMessage>
        {
            public Task Handle(OutgoingWithComplexHierarchyMessage message, IMessageHandlerContext context)
            {
                var numberOfMessages = Interlocked.Increment(ref testContext.ComplexOutgoingMessagesReceived);
                testContext.MarkAsCompleted(numberOfMessages == 5);
                return Task.CompletedTask;
            }
        }
    }

    public class OutgoingMessage : IMessage;

    public class BaseOutgoingMessage : IMessage;

    public class OutgoingWithComplexHierarchyMessage : BaseOutgoingMessage;
}