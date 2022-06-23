namespace NServiceBus.AcceptanceTests.Diagnostics
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;
    using NServiceBus;

    [NonParallelizable]
    public class When_messages_processed_successfully : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_report_successful_message_metric()
        {
            using var metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();
            _ = await Scenario.Define<Context>()
                .WithEndpoint<TestEndpoint>(b => b
                    .CustomConfig(cfg => cfg.EnableOpenTelemetryMetrics())
                    .When(async (session, ctx) =>
                    {
                        for (var x = 0; x < 5; x++)
                        {
                            await session.SendLocal(new OutgoingMessage
                            {
                                Id = ctx.TestRunId
                            });
                        }
                    }))
                .Done(c => c.OutgoingMessagesReceived == 5)
                .Run();

            metricsListener.AssertMetric("messaging.successes", 5);
            metricsListener.AssertMetric("messaging.fetches", 5);
            metricsListener.AssertMetric("messaging.failures", 0);

            metricsListener.AssertTagValue("messaging.successes", "messaging.endpoint", "EndpointWithMetrics");
            metricsListener.AssertTagValue("messaging.successes", "messaging.queue", "EndpointWithMetrics");
            metricsListener.AssertTagValue("messaging.successes", "messaging.type", "NServiceBus.AcceptanceTests.Diagnostics.When_messages_processed_successfully+OutgoingMessage, NServiceBus.AcceptanceTests, Version=8.0.0.0, Culture=neutral, PublicKeyToken=null");

            metricsListener.AssertTagValue("messaging.fetches", "messaging.endpoint", "EndpointWithMetrics");
            metricsListener.AssertTagValue("messaging.fetches", "messaging.queue", "EndpointWithMetrics");
            metricsListener.AssertTagValue("messaging.fetches", "messaging.type", "NServiceBus.AcceptanceTests.Diagnostics.When_messages_processed_successfully+OutgoingMessage, NServiceBus.AcceptanceTests, Version=8.0.0.0, Culture=neutral, PublicKeyToken=null");
        }

        class Context : ScenarioContext
        {
            public int OutgoingMessagesReceived;
        }

        class TestEndpoint : EndpointConfigurationBuilder
        {
            public TestEndpoint() => EndpointSetup<DefaultServer>().CustomEndpointName("EndpointWithMetrics");

            class MessageHandler : IHandleMessages<OutgoingMessage>
            {
                Context testContext;

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
            public Guid Id { get; set; }
        }
    }
}