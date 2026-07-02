namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Metrics;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EndpointTemplates;
using NServiceBus;
using AcceptanceTesting;
using NUnit.Framework;
using Conventions = AcceptanceTesting.Customization.Conventions;

public class When_handlers_are_invoked : OpenTelemetryAcceptanceTest
{
    const string ActiveHandlersMetric = "nservicebus.messaging.active_handlers";
    const int numberOfMessages = 5;

    [Test]
    public async Task Should_report_active_handlers_gauge_that_balances_once_idle()
    {
        using var metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();

        _ = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithMetrics>(b => b.CustomConfig(c =>
                {
                    c.MakeInstanceUniquelyAddressable("instanceId");
                    c.LimitMessageProcessingConcurrencyTo(10);
                }).When(async (session, ctx) =>
                {
                    for (var x = 0; x < numberOfMessages; x++)
                    {
                        await session.SendLocal(new OutgoingMessage());
                    }
                }))
            .Run();

        Assert.That(metricsListener.ReportedMeters.TryGetValue(ActiveHandlersMetric, out var net), Is.True,
            $"'{ActiveHandlersMetric}' gauge should be reported");
        Assert.That(net, Is.EqualTo(0),
            "increments and decrements should balance once all handlers have completed");

        metricsListener.AssertTags(ActiveHandlersMetric,
            new Dictionary<string, object>
            {
                ["nservicebus.queue"] = Conventions.EndpointNamingConvention(typeof(EndpointWithMetrics)),
                ["nservicebus.discriminator"] = "instanceId",
                ["nservicebus.message_type"] = typeof(OutgoingMessage).FullName,
                ["nservicebus.message_handler_type"] = typeof(EndpointWithMetrics.MessageHandler).FullName
            });
    }

    public class Context : ScenarioContext
    {
        public int OutgoingMessagesReceived;
    }

    public class EndpointWithMetrics : EndpointConfigurationBuilder
    {
        public EndpointWithMetrics() => EndpointSetup<DefaultServer>();

        [Handler]
        public class MessageHandler(Context testContext) : IHandleMessages<OutgoingMessage>
        {
            public Task Handle(OutgoingMessage message, IMessageHandlerContext context)
            {
                var messagesHandled = Interlocked.Increment(ref testContext.OutgoingMessagesReceived);
                testContext.MarkAsCompleted(messagesHandled == numberOfMessages);
                return Task.CompletedTask;
            }
        }
    }

    public class OutgoingMessage : IMessage;
}
