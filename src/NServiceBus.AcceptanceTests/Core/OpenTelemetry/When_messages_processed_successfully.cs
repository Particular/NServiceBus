namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry;

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
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
                .When(async (session, ctx) =>
                {
                    for (var x = 0; x < 5; x++)
                    {
                        await session.SendLocal(new OutgoingMessage());
                    }
                }))
            .Done(c => c.OutgoingMessagesReceived == 5)
            .Run();

        metricsListener.AssertMetricNotReported("nservicebus.messaging.failures");

        //metricsListener.AssertMetric("nservicebus.messaging.fetches", 5);
        var successMeasurements = metricsListener.GetReportedMeasurements<long>("nservicebus.messaging.successes");

        Assert.AreEqual(5, successMeasurements.Sum(m => m.Value));

        var successMeasurement = successMeasurements.First();

        var successQueueName = successMeasurement.Tags.ToArray().First(kvp => kvp.Key == "nservicebus.queue").Value;
        var successType = successMeasurement.Tags.ToArray().First(kvp => kvp.Key == "nservicebus.message_type").Value;

        //var fetchedEndpoint = metricsListener.AssertTagKeyExists("nservicebus.messaging.fetches", "nservicebus.queue");
        //var fetchedType = metricsListener.AssertTagKeyExists("nservicebus.messaging.fetches", "nservicebus.message_type").ToString();
        var enpointName = Conventions.EndpointNamingConvention(typeof(EndpointWithMetrics));
        Assert.AreEqual(enpointName, successQueueName);
        //Assert.AreEqual(Conventions.EndpointNamingConvention(typeof(EndpointWithMetrics)), fetchedEndpoint);
        Assert.AreEqual(successType, typeof(OutgoingMessage).AssemblyQualifiedName);
        //Assert.AreEqual(fetchedType, typeof(OutgoingMessage).AssemblyQualifiedName);
    }

    class Context : ScenarioContext
    {
        public int OutgoingMessagesReceived;
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
    }

    public class OutgoingMessage : IMessage
    {
    }
}