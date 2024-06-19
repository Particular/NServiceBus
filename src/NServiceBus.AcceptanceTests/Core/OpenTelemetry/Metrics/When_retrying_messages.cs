namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Metrics;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
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
            .Done(c => c.InvocationCounter == 2)
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
            .Done(c => c.InvocationCounter == 2)
            .Run();

        metricsListener.AssertMetric("nservicebus.recoverability.immediate", 0);
        metricsListener.AssertMetric("nservicebus.recoverability.delayed", 1);
        metricsListener.AssertMetric("nservicebus.recoverability.error", 0);
    }

    [Test]
    public async Task Should_increment_error_meter()
    {
        using var metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();

        await Scenario.Define<Context>()
            .WithEndpoint<RetryingEndpoint>(e => e
                .CustomConfig(c =>
                {
                    c.Recoverability().Immediate(i => i.NumberOfRetries(0));
                    c.Recoverability().Delayed(i => i.NumberOfRetries(0));
                })
                .DoNotFailOnErrorMessages()
                .When(s => s.SendLocal(new FailingMessage())))
            .Done(c => c.FailedMessages.Count == 1)
            .Run();

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
            var template = new OpenTelemetryEnabledEndpoint
            {
                TransportConfiguration = new ConfigureEndpointAcceptanceTestingTransport(false, true)
            };
            EndpointSetup(template, (endpointConfiguration, descriptor) => { });
        }

        class Handler : IHandleMessages<FailingMessage>
        {
            Context testContext;

            public Handler(Context testContext)
            {
                this.testContext = testContext;
            }

            public Task Handle(FailingMessage message, IMessageHandlerContext context)
            {
                testContext.InvocationCounter++;

                if (testContext.InvocationCounter == 1)
                {
                    throw new SimulatedException("first attempt fails");
                }

                return Task.CompletedTask;
            }
        }
    }

    public class FailingMessage : IMessage
    {
    }
}