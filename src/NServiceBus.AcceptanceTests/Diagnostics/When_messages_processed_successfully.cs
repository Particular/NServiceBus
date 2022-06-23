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

            var expectedEndpoint = metricsListener.AssertTagKeyExists("messaging.successes", "messaging.endpoint");
            metricsListener.AssertTagKeyExists("messaging.successes", "messaging.queue");
            metricsListener.AssertTagKeyExists("messaging.fetches", "messaging.endpoint");
            metricsListener.AssertTagKeyExists("messaging.fetches", "messaging.queue");
            Assert.AreEqual("EndpointWithMetrics", expectedEndpoint);

            var expectedType = metricsListener.AssertTagKeyExists("messaging.fetches", "messaging.type").ToString();
            metricsListener.AssertTagKeyExists("messaging.successes", "messaging.type");
            Assert.True(expectedType.Contains("When_messages_processed_successfully+OutgoingMessage"));
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