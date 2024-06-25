namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry;

using System;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using Metrics;
using NUnit.Framework;
using Conventions = AcceptanceTesting.Customization.Conventions;

public class When_incoming_message_handled : OpenTelemetryAcceptanceTest
{
    static readonly string HandlerTimeMetricName = "nservicebus.messaging.handler_time";

    [Test]
    public async Task Should_record_success_handling_time()
    {
        using TestingMetricListener metricsListener = await WhenMessagesHandled(() => new MyMessage());
        metricsListener.AssertMetric(HandlerTimeMetricName, 5);
        AssertMandatoryTags(metricsListener, HandlerTimeMetricName, typeof(MyMessage));
        var handlerType = metricsListener.AssertTagKeyExists(HandlerTimeMetricName, "nservicebus.message_handler_type");
        Assert.AreEqual(typeof(MyMessageHandler).FullName, handlerType);
        var result = metricsListener.AssertTagKeyExists(HandlerTimeMetricName, "execution.result");
        Assert.AreEqual("success", result);
    }

    [Test]
    public async Task Should_record_failure_handling_time()
    {
        using TestingMetricListener metricsListener = await WhenMessagesHandled(() => new MyExceptionalMessage());
        metricsListener.AssertMetric(HandlerTimeMetricName, 5);
        AssertMandatoryTags(metricsListener, HandlerTimeMetricName, typeof(MyExceptionalMessage));
        var handlerType = metricsListener.AssertTagKeyExists(HandlerTimeMetricName, "nservicebus.message_handler_type");
        Assert.AreEqual(typeof(MyExceptionalHandler).FullName, handlerType);
        var error = metricsListener.AssertTagKeyExists(HandlerTimeMetricName, "error.type");
        Assert.AreEqual(typeof(Exception).FullName, error);
        var result = metricsListener.AssertTagKeyExists(HandlerTimeMetricName, "execution.result");
        Assert.AreEqual("failure", result);
    }

    static async Task<TestingMetricListener> WhenMessagesHandled(Func<IMessage> messageFactory)
    {
        TestingMetricListener metricsListener = null;
        try
        {
            metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();

            _ = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithMetrics>(b =>
                    b.DoNotFailOnErrorMessages()
                        .CustomConfig(c => c.MakeInstanceUniquelyAddressable("discriminator"))
                        .When(async (session, _) =>
                        {
                            for (var x = 0; x < 5; x++)
                            {
                                try
                                {
                                    await session.SendLocal(messageFactory.Invoke());
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e);
                                }
                            }
                        }))
                .Done(c => c.TotalHandledMessages == 5)
                .Run();
            return metricsListener;
        }
        catch
        {
            metricsListener?.Dispose();
            throw;
        }
    }


    static void AssertMandatoryTags(
        TestingMetricListener metricsListener,
        string metricName,
        Type expectedMessageType)
    {
        var messageType = metricsListener.AssertTagKeyExists(metricName, "nservicebus.message_type");
        Assert.AreEqual(expectedMessageType.FullName, messageType);
        var endpoint = metricsListener.AssertTagKeyExists(metricName, "nservicebus.queue");
        Assert.AreEqual(Conventions.EndpointNamingConvention(typeof(EndpointWithMetrics)), endpoint);
        var discriminator = metricsListener.AssertTagKeyExists(metricName, "nservicebus.discriminator");
        Assert.AreEqual("discriminator", discriminator);
    }

    class Context : ScenarioContext
    {
        public int TotalHandledMessages;
    }

    class EndpointWithMetrics : EndpointConfigurationBuilder
    {
        public EndpointWithMetrics() => EndpointSetup<OpenTelemetryEnabledEndpoint>();
    }

    class MyMessageHandler : IHandleMessages<MyMessage>
    {
        readonly Context testContext;

        public MyMessageHandler(Context testContext) => this.testContext = testContext;

        public Task Handle(MyMessage message, IMessageHandlerContext context)
        {
            Interlocked.Increment(ref testContext.TotalHandledMessages);
            return Task.CompletedTask;
        }
    }

    class MyExceptionalHandler : IHandleMessages<MyExceptionalMessage>
    {
        readonly Context testContext;

        public MyExceptionalHandler(Context testContext) => this.testContext = testContext;

        public Task Handle(MyExceptionalMessage message, IMessageHandlerContext context)
        {
            Interlocked.Increment(ref testContext.TotalHandledMessages);
            throw new Exception();
        }
    }

    public class MyMessage : IMessage
    {
    }

    public class MyExceptionalMessage : IMessage
    {
    }
}