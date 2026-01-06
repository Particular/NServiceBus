namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry;

using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Metrics;
using NUnit.Framework;
using Traces;

public class When_OpenTelemetry_disabled : OpenTelemetryAcceptanceTest
{
    [Test]
    public async Task Should_still_report_metrics()
    {
        using var metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();

        await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithOpenTelemetryDisabled>(b =>
                b.When(async (session, _) => await session.SendLocal(new MyMessage())))
            .Run();

        metricsListener.AssertMetric("nservicebus.messaging.successes", 1);
        metricsListener.AssertMetric("nservicebus.messaging.fetches", 1);
        metricsListener.AssertMetric("nservicebus.messaging.failures", 0);
        metricsListener.AssertMetric("nservicebus.messaging.critical_time", 1);
        metricsListener.AssertMetric("nservicebus.messaging.processing_time", 1);
        metricsListener.AssertMetric("nservicebus.messaging.handler_time", 1);
    }

    [Test]
    public async Task Should_not_record_activities()
    {
        using var activityListener = TestingActivityListener.SetupDiagnosticListener("NServiceBus.Core");

        await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithOpenTelemetryDisabled>(b =>
                b.When(async (session, _) => await session.SendLocal(new MyMessage())))
            .Run();

        activityListener.VerifyAllActivitiesCompleted();
        Assert.That(activityListener.CompletedActivities, Is.Empty, "No activities should be recorded when OpenTelemetry is disabled");
    }

    class Context : ScenarioContext;

    class EndpointWithOpenTelemetryDisabled : EndpointConfigurationBuilder
    {
        public EndpointWithOpenTelemetryDisabled() =>
            EndpointSetup<DefaultServer>(c =>
            {
                c.DisableOpenTelemetry();
            });

        class MyMessageHandler(Context testContext) : IHandleMessages<MyMessage>
        {
            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class MyMessage : IMessage;
}