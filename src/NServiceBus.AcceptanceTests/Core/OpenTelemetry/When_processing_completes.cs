namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry;

using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NUnit.Framework;

public class When_processing_completes : OpenTelemetryAcceptanceTest
{
    [Test]
    public async Task Should_report_processing_and_critical_time()
    {
        using var metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();
        _ = await Scenario.Define<Context>()
            .WithEndpoint<MyEndpoint>(e => e
                .When(s => s.SendLocal(new MyMessage())))
            .Done(c => c.HandlerInvoked)
            .Run();

        var processingTime = metricsListener.GetReportedMeasurements<double>("nservicebus.messaging.processingtime").Single();
        var criticalTime = metricsListener.GetReportedMeasurements<double>("nservicebus.messaging.criticaltime").Single();

        Assert.Greater(processingTime.Value, 50.0);
        Assert.Greater(criticalTime.Value, processingTime.Value);
    }

    class Context : ScenarioContext
    {
        public bool HandlerInvoked { get; set; }
    }

    class MyEndpoint : EndpointConfigurationBuilder
    {
        public MyEndpoint() => EndpointSetup<OpenTelemetryEnabledEndpoint>();

        class MyMessageHandler : IHandleMessages<MyMessage>
        {
            Context textContext;

            public MyMessageHandler(Context textContext) => this.textContext = textContext;

            public async Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                await Task.Delay(50);

                textContext.HandlerInvoked = true;
            }
        }
    }

    public class MyMessage : IMessage
    {
    }
}