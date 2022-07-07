namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry
{
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;
    using Conventions = AcceptanceTesting.Customization.Conventions;

    public class When_messages_processed_successfully : OpenTelemetryAcceptanceTest
    {
        [Test]
        public async Task Should_report_successful_message_metric()
        {
            using var metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();

            _ = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithMetrics>(b => b
                    .CustomConfig(cfg => cfg.EnableOpenTelemetryMetrics())
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

            var successEndpoint = metricsListener.AssertTagKeyExists("nservicebus.messaging.successes", "nservicebus.queue");
            var successType = metricsListener.AssertTagKeyExists("nservicebus.messaging.successes", "nservicebus.type");
            var fetchedEndpoint = metricsListener.AssertTagKeyExists("nservicebus.messaging.fetches", "nservicebus.queue");
            var fetchedType = metricsListener.AssertTagKeyExists("nservicebus.messaging.fetches", "nservicebus.type").ToString();

            Assert.AreEqual(Conventions.EndpointNamingConvention(typeof(EndpointWithMetrics)), successEndpoint);
            Assert.AreEqual(Conventions.EndpointNamingConvention(typeof(EndpointWithMetrics)), fetchedEndpoint);
            Assert.AreEqual(successType, typeof(OutgoingMessage).AssemblyQualifiedName);
            Assert.AreEqual(fetchedType, typeof(OutgoingMessage).AssemblyQualifiedName);
        }

        class Context : ScenarioContext
        {
            public int OutgoingMessagesReceived;
        }

        class EndpointWithMetrics : EndpointConfigurationBuilder
        {
            public EndpointWithMetrics() => EndpointSetup<DefaultServer>();

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
        }

        public class OutgoingMessage : IMessage
        {
        }
    }
}