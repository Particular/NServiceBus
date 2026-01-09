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
    const int NumberOfMessages = 5;

    [Test]
    public async Task Should_record_handling_time()
    {
        using var metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();

        _ = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithMetrics>(b =>
                b.When(async session =>
                {
                    for (var x = 0; x < NumberOfMessages; x++)
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
        Assert.That(handlerType, Is.EqualTo(typeof(MyMessageHandler).FullName));
        var endpoint = metricsListener.AssertTagKeyExists(handlingTime, "nservicebus.queue");
        Assert.That(endpoint, Is.EqualTo(Conventions.EndpointNamingConvention(typeof(EndpointWithMetrics))));
    }

    class Context : ScenarioContext
    {
        public void MaybeCompleted() => MarkAsCompleted(Interlocked.Increment(ref TotalHandledMessages) == NumberOfMessages);

        int TotalHandledMessages;
    }

    class EndpointWithMetrics : EndpointConfigurationBuilder
    {
        public EndpointWithMetrics() => EndpointSetup<DefaultServer>();
    }

    class MyMessageHandler(Context testContext) : IHandleMessages<MyMessage>
    {
        public Task Handle(MyMessage message, IMessageHandlerContext context)
        {
            testContext.MaybeCompleted();
            return Task.CompletedTask;
        }
    }

    public class MyMessage : IMessage;
}