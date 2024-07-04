namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Metrics;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NUnit.Framework;
using Conventions = AcceptanceTesting.Customization.Conventions;

public class When_message_is_processed_successfully : OpenTelemetryAcceptanceTest
{
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
            .Done(c => c.OutgoingMessagesReceived == 5)
            .Run();

        metricsListener.AssertMetric("nservicebus.messaging.successes", 5);
        metricsListener.AssertMetric("nservicebus.messaging.fetches", 5);
        metricsListener.AssertMetric("nservicebus.messaging.failures", 0);

        metricsListener.AssertTags("nservicebus.messaging.fetches",
            new Dictionary<string, object>
            {
                ["nservicebus.queue"] = Conventions.EndpointNamingConvention(typeof(EndpointWithMetrics)),
                ["nservicebus.discriminator"] = "disc",
            });

        metricsListener.AssertTags("nservicebus.messaging.successes",
            new Dictionary<string, object>
            {
                ["nservicebus.queue"] = Conventions.EndpointNamingConvention(typeof(EndpointWithMetrics)),
                ["nservicebus.discriminator"] = "disc",
                ["nservicebus.message_type"] = typeof(OutgoingMessage).FullName,
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
            .Done(c => c.ComplexOutgoingMessagesReceived == 5)
            .Run();

        metricsListener.AssertMetric("nservicebus.messaging.successes", 5);
        metricsListener.AssertMetric("nservicebus.messaging.fetches", 5);
        metricsListener.AssertMetric("nservicebus.messaging.failures", 0);

        var successEndpoint = metricsListener.AssertTagKeyExists("nservicebus.messaging.successes", "nservicebus.queue");
        var successType = metricsListener.AssertTagKeyExists("nservicebus.messaging.successes", "nservicebus.message_type");
        var successHandlerType = metricsListener.AssertTagKeyExists("nservicebus.messaging.successes", "nservicebus.message_handler_types");

        var fetchedEndpoint = metricsListener.AssertTagKeyExists("nservicebus.messaging.fetches", "nservicebus.queue");

        Assert.AreEqual(Conventions.EndpointNamingConvention(typeof(EndpointWithMetrics)), successEndpoint);
        Assert.AreEqual(Conventions.EndpointNamingConvention(typeof(EndpointWithMetrics)), fetchedEndpoint);
        Assert.AreEqual(typeof(OutgoingWithComplexHierarchyMessage).FullName, successType);
        Assert.AreEqual(typeof(EndpointWithMetrics.ComplexMessageHandler).FullName, successHandlerType);
    }

    class Context : ScenarioContext
    {
        public int OutgoingMessagesReceived;
        public int ComplexOutgoingMessagesReceived;
    }

    class EndpointWithMetrics : EndpointConfigurationBuilder
    {
        public EndpointWithMetrics() => EndpointSetup<OpenTelemetryEnabledEndpoint>();

        class MessageHandler : IHandleMessages<OutgoingMessage>
        {
            readonly Context testContext;

            public MessageHandler(Context testContext) => this.testContext = testContext;

            public Task Handle(OutgoingMessage message, IMessageHandlerContext context)
            {
                Interlocked.Increment(ref testContext.OutgoingMessagesReceived);
                return Task.CompletedTask;
            }
        }

        public class ComplexMessageHandler : IHandleMessages<OutgoingWithComplexHierarchyMessage>
        {
            readonly Context testContext;

            public ComplexMessageHandler(Context testContext) => this.testContext = testContext;

            public Task Handle(OutgoingWithComplexHierarchyMessage message, IMessageHandlerContext context)
            {
                Interlocked.Increment(ref testContext.ComplexOutgoingMessagesReceived);
                return Task.CompletedTask;
            }
        }
    }

    public class OutgoingMessage : IMessage
    {
    }

    public class BaseOutgoingMessage : IMessage
    {
    }

    public class OutgoingWithComplexHierarchyMessage : BaseOutgoingMessage
    {
    }
}