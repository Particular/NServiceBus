namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry;

using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Metrics;
using NUnit.Framework;

public class When_OpenTelemetry_disabled : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_not_record_metrics()
    {
        using var metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();

        await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithOpenTelemetryDisabled>(b =>
                b.When(async (session, _) =>
                {
                    for (var x = 0; x < 5; x++)
                    {
                        await session.SendLocal(new MyMessage());
                    }
                }))
            .Done(c => c.HandledMessages >= 5)
            .Run();

        metricsListener.AssertMetric("nservicebus.messaging.handler_time", 0);
        metricsListener.AssertMetric("nservicebus.messaging.critical_time", 0);
    }

    [Test]
    public async Task Should_not_record_activities()
    {
        using var activityListener = TestingActivityListener.SetupDiagnosticListener("NServiceBus.Core");

        await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithOpenTelemetryDisabled>(b =>
                b.When(async (session, _) =>
                {
                    await session.SendLocal(new MyMessage());
                }))
            .Done(c => c.HandledMessages >= 1)
            .Run();

        activityListener.VerifyAllActivitiesCompleted();
        Assert.That(activityListener.CompletedActivities, Is.Empty, "No activities should be recorded when OpenTelemetry is disabled");
    }

    class Context : ScenarioContext
    {
        public int HandledMessages { get; set; }
    }

    class EndpointWithOpenTelemetryDisabled : EndpointConfigurationBuilder
    {
        public EndpointWithOpenTelemetryDisabled()
        {
            EndpointSetup<DefaultServer>(c =>
            {
                c.DisableOpenTelemetry();
            });
        }

        class MyMessageHandler : IHandleMessages<MyMessage>
        {
            readonly Context testContext;

            public MyMessageHandler(Context testContext) => this.testContext = testContext;

            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                testContext.HandledMessages++;
                return Task.CompletedTask;
            }
        }
    }

    public class MyMessage : IMessage
    {
    }
}
