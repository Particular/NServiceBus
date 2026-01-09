namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry;

using System;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Metrics;
using NUnit.Framework;
using Conventions = AcceptanceTesting.Customization.Conventions;

[NonParallelizable]
public class When_incoming_message_handled : NServiceBusAcceptanceTest
{
    static readonly string HandlerTimeMetricName = "nservicebus.messaging.handler_time";
    static readonly string CriticalTimeMetricName = "nservicebus.messaging.critical_time";
    const int NumberOfMessages = 5;

    [Test]
    public async Task Should_record_critical_time()
    {
        using var metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();

        await WhenMessagesHandled(() => new MyMessage());
        metricsListener.AssertMetric(CriticalTimeMetricName, NumberOfMessages);
        AssertMandatoryTags(metricsListener, CriticalTimeMetricName, typeof(MyMessage));
    }

    [Test]
    public async Task Should_record_success_handling_time()
    {
        using var metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();

        await WhenMessagesHandled(() => new MyMessage());
        metricsListener.AssertMetric(HandlerTimeMetricName, NumberOfMessages);
        AssertMandatoryTags(metricsListener, HandlerTimeMetricName, typeof(MyMessage));
        var handlerType = metricsListener.AssertTagKeyExists(HandlerTimeMetricName, "nservicebus.message_handler_type");
        Assert.That(handlerType, Is.EqualTo(typeof(MyMessageHandler).FullName));
        var result = metricsListener.AssertTagKeyExists(HandlerTimeMetricName, "execution.result");
        Assert.That(result, Is.EqualTo("success"));
    }

    [Test]
    public async Task Should_record_failure_handling_time()
    {
        using var metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();

        await WhenMessagesHandled(() => new MyExceptionalMessage());
        metricsListener.AssertMetric(HandlerTimeMetricName, NumberOfMessages);
        AssertMandatoryTags(metricsListener, HandlerTimeMetricName, typeof(MyExceptionalMessage));
        var handlerType = metricsListener.AssertTagKeyExists(HandlerTimeMetricName, "nservicebus.message_handler_type");
        Assert.That(handlerType, Is.EqualTo(typeof(MyExceptionalHandler).FullName));
        var error = metricsListener.AssertTagKeyExists(HandlerTimeMetricName, "error.type");
        Assert.That(error, Is.EqualTo(typeof(Exception).FullName));
        var result = metricsListener.AssertTagKeyExists(HandlerTimeMetricName, "execution.result");
        Assert.That(result, Is.EqualTo("failure"));
    }

    [Test]
    public async Task Should_not_record_critical_time_on_failure()
    {
        using var metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();
        await WhenMessagesHandled(() => new MyExceptionalMessage());
        metricsListener.AssertMetric(CriticalTimeMetricName, 0);
    }

    static Task<Context> WhenMessagesHandled(Func<IMessage> messageFactory) =>
        Scenario.Define<Context>()
            .WithEndpoint<EndpointWithMetrics>(b =>
                b.DoNotFailOnErrorMessages()
                    .CustomConfig(c => c.MakeInstanceUniquelyAddressable("discriminator"))
                    .When(async session =>
                    {
                        for (var x = 0; x < NumberOfMessages; x++)
                        {
                            await session.SendLocal(messageFactory.Invoke());
                        }
                    }))
            .Run();

    static void AssertMandatoryTags(
        TestingMetricListener metricsListener,
        string metricName,
        Type expectedMessageType)
    {
        var messageType = metricsListener.AssertTagKeyExists(metricName, "nservicebus.message_type");
        Assert.That(messageType, Is.EqualTo(expectedMessageType.FullName));
        var endpoint = metricsListener.AssertTagKeyExists(metricName, "nservicebus.queue");
        Assert.That(endpoint, Is.EqualTo(Conventions.EndpointNamingConvention(typeof(EndpointWithMetrics))));
        var discriminator = metricsListener.AssertTagKeyExists(metricName, "nservicebus.discriminator");
        Assert.That(discriminator, Is.EqualTo("discriminator"));
    }

    class Context : ScenarioContext
    {
        public void MaybeCompleted() => MarkAsCompleted(Interlocked.Increment(ref totalHandledMessages) == NumberOfMessages);

        int totalHandledMessages;
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

    class MyExceptionalHandler(Context testContext) : IHandleMessages<MyExceptionalMessage>
    {
        public Task Handle(MyExceptionalMessage message, IMessageHandlerContext context)
        {
            testContext.MaybeCompleted();
            throw new Exception();
        }
    }

    public class MyMessage : IMessage;

    public class MyExceptionalMessage : IMessage;
}