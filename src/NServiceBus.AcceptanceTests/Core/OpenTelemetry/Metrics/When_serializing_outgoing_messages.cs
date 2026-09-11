namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Metrics;

using System.Collections.Generic;
using System.Threading.Tasks;
using EndpointTemplates;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Customization;
using NUnit.Framework;
using Conventions = AcceptanceTesting.Customization.Conventions;

public class When_serializing_outgoing_messages : OpenTelemetryAcceptanceTest
{
    const string MessageSerializeTime = "nservicebus.messaging.serialize_time";

    [Test]
    public async Task Should_tag_serialize_time_with_the_queue_and_discriminator()
    {
        using var metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();

        _ = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithMetrics>(b => b
                .CustomConfig(x => x.MakeInstanceUniquelyAddressable("disc"))
                .When(session => session.SendLocal(new OutgoingMessage())))
            .Run();

        metricsListener.AssertTags(MessageSerializeTime,
            new Dictionary<string, object>
            {
                ["nservicebus.queue"] = Conventions.EndpointNamingConvention(typeof(EndpointWithMetrics)),
                ["nservicebus.discriminator"] = "disc",
                ["nservicebus.message_type"] = typeof(OutgoingMessage).FullName
            });
    }

    [Test]
    public async Task Should_not_tag_serialize_time_with_the_queue_and_discriminator_when_send_only()
    {
        // A send-only endpoint never creates its input queue, so reporting one would be misleading.
        using var metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();

        _ = await Scenario.Define<Context>()
            .WithEndpoint<SendOnlyEndpoint>(b => b.When(session => session.Send(new OutgoingMessage())))
            .WithEndpoint<EndpointWithMetrics>()
            .Run();

        //using (Assert.EnterMultipleScope())
        {
            metricsListener.AssertTagKeyDoesNotExist(MessageSerializeTime, "nservicebus.queue");
            metricsListener.AssertTagKeyDoesNotExist(MessageSerializeTime, "nservicebus.discriminator");
            // the metric itself is still recorded, only the two tags are unavailable
            Assert.That(metricsListener.AssertTagKeyExists(MessageSerializeTime, "nservicebus.message_type"),
                Is.EqualTo(typeof(OutgoingMessage).FullName));
        }
    }

    public class Context : ScenarioContext;

    public class SendOnlyEndpoint : EndpointConfigurationBuilder
    {
        public SendOnlyEndpoint() =>
            EndpointSetup<DefaultServer>(c =>
            {
                c.SendOnly();
                c.ConfigureRouting().RouteToEndpoint(typeof(OutgoingMessage), typeof(EndpointWithMetrics));
            });
    }

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
