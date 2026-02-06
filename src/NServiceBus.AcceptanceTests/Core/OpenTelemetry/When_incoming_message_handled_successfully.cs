namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry;

using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Metrics;
using NUnit.Framework;
using Conventions = AcceptanceTesting.Customization.Conventions;

[NonParallelizable]
public class WhenIncomingMessageHandledSuccessfully : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_record_handling_time()
    {
        using var metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();

        _ = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithMetrics>(b =>
                b.When(async (session, _) =>
                {
                    for (var x = 0; x < 5; x++)
                    {
                        await session.SendLocal(new MyMessage());
                    }
                }))
            .Run();

        string handlingTime = "nservicebus.messaging.handler_time";
        metricsListener.AssertMetric(handlingTime, 5);
        var messageType = metricsListener.AssertTagKeyExists(handlingTime, "nservicebus.message_type");
        Assert.That(messageType, Is.EqualTo(typeof(MyMessage).FullName));
        var handlerType = metricsListener.AssertTagKeyExists(handlingTime, "nservicebus.message_handler_type");
        Assert.That(handlerType, Is.EqualTo(typeof(EndpointWithMetrics.MyMessageHandler).FullName));
        var endpoint = metricsListener.AssertTagKeyExists(handlingTime, "nservicebus.queue");
        Assert.That(endpoint, Is.EqualTo(Conventions.EndpointNamingConvention(typeof(EndpointWithMetrics))));
    }

    public class Context : ScenarioContext
    {
        public int TotalHandledMessages;
    }

    public class EndpointWithMetrics : EndpointConfigurationBuilder
    {
        public EndpointWithMetrics() => EndpointSetup<DefaultServer>();

        [Handler]
        public class MyMessageHandler(Context testContext) : IHandleMessages<MyMessage>
        {
            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                var count = Interlocked.Increment(ref testContext.TotalHandledMessages);
                testContext.MarkAsCompleted(count == 5);
                return Task.CompletedTask;
            }
        }
    }

    public class MyMessage : IMessage;
}