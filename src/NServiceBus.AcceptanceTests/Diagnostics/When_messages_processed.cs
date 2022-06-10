using System;
using System.Threading.Tasks;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NUnit.Framework;

namespace NServiceBus.AcceptanceTests.Diagnostics
{
    public class When_messages_processed : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_report_successful_message_metric()
        {
            using var metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();
            _ = await Scenario.Define<Context>()
                .WithEndpoint<TestEndpoint>(b => b
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
        }

        class Context : ScenarioContext
        {
            public int OutgoingMessagesReceived { get; set; }
        }

        class TestEndpoint : EndpointConfigurationBuilder
        {
            public TestEndpoint() => EndpointSetup<DefaultServer>();

            class MessageHandler : IHandleMessages<OutgoingMessage>
            {
                Context testContext;

                public MessageHandler(Context testContext) => this.testContext = testContext;

                public Task Handle(OutgoingMessage message, IMessageHandlerContext context)
                {
                    testContext.OutgoingMessagesReceived += 1;
                    return Task.CompletedTask;
                }
            }
        }

        public class OutgoingMessage : IMessage
        {
            public Guid Id { get; set; }
        }
    }
    
    static class AssertHelper
    {
        public static void AssertMetric(this TestingMetricListener listener, string metricName, long expected)
        {
            if (expected == 0)
            {
                Assert.False(listener.ReportedMeters.ContainsKey(metricName));
            }
            else
            {
                Assert.AreEqual(expected, listener.ReportedMeters[metricName]);
            }
        }
    }
}