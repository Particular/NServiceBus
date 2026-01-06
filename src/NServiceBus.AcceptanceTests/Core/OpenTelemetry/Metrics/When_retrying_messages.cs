namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Metrics;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_retrying_messages : OpenTelemetryAcceptanceTest
{
    [Test]
    public async Task Should_increment_immediate_meter()
    {
        using var metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();

        await Scenario.Define<Context>()
            .WithEndpoint<RetryingEndpoint>(e => e
                .CustomConfig(c => c.Recoverability().Immediate(i => i.NumberOfRetries(1)))
                .DoNotFailOnErrorMessages()
                .When(s => s.SendLocal(new FailingMessage())))
            .Run();

        metricsListener.AssertMetric("nservicebus.recoverability.immediate", 1);
        metricsListener.AssertMetric("nservicebus.recoverability.delayed", 0);
        metricsListener.AssertMetric("nservicebus.recoverability.error", 0);
    }

    [Test]
    public async Task Should_increment_delayed_meter()
    {
        //Requires.DelayedDelivery();

        using var metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();

        await Scenario.Define<Context>()
            .WithEndpoint<RetryingEndpoint>(e => e
                .CustomConfig(c =>
                {
                    c.Recoverability().Immediate(i => i.NumberOfRetries(0));
                    c.Recoverability().Delayed(i => i.NumberOfRetries(1).TimeIncrease(TimeSpan.FromMilliseconds(1)));
                })
                .DoNotFailOnErrorMessages()
                .When(s => s.SendLocal(new FailingMessage())))
            .Run();

        metricsListener.AssertMetric("nservicebus.recoverability.immediate", 0);
        metricsListener.AssertMetric("nservicebus.recoverability.delayed", 1);
        metricsListener.AssertMetric("nservicebus.recoverability.error", 0);
    }

    [Test]
    public void Should_increment_error_meter()
    {
        using var metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();

        Assert.That(async () =>
        {
            await Scenario.Define<Context>()
                .WithEndpoint<RetryingEndpoint>(e => e
                    .CustomConfig(c =>
                    {
                        c.Recoverability().Immediate(i => i.NumberOfRetries(0));
                        c.Recoverability().Delayed(i => i.NumberOfRetries(0));
                    })
                    .When(s => s.SendLocal(new FailingMessage())))
                .Run();
        }, Throws.Exception);

        metricsListener.AssertMetric("nservicebus.recoverability.immediate", 0);
        metricsListener.AssertMetric("nservicebus.recoverability.delayed", 0);
        metricsListener.AssertMetric("nservicebus.recoverability.error", 1);
    }

    class Context : ScenarioContext
    {
        public int InvocationCounter { get; set; }
    }

    class RetryingEndpoint : EndpointConfigurationBuilder
    {
        public RetryingEndpoint()
        {
            var template = new DefaultServer
            {
                TransportConfiguration = new ConfigureEndpointAcceptanceTestingTransport(false, true)
            };
            EndpointSetup(template, (_, _) => { });
        }

        class Handler(Context testContext) : IHandleMessages<FailingMessage>
        {
            public Task Handle(FailingMessage message, IMessageHandlerContext context)
            {
                testContext.InvocationCounter++;

                if (testContext.InvocationCounter == 1)
                {
                    throw new SimulatedException("first attempt fails");
                }

                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class FailingMessage : IMessage;
}