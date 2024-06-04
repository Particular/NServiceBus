namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry;

using System;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using NUnit.Framework;
using Conventions = AcceptanceTesting.Customization.Conventions;

public class When_incoming_message_handled : OpenTelemetryAcceptanceTest
{
    static readonly string HandlingTimeMetricName = "nservicebus.messaging.handling_time";

    [Test]
    public async Task Should_record_success_handling_time()
    {
        using TestingMetricListener metricsListener = await WhenMessagesHandled(() => new MyMessage());
        metricsListener.AssertMetric(HandlingTimeMetricName, 5);
        AsserMandatoryTags(metricsListener, typeof(MyMessage), typeof(MyMessageHandler));
    }

    [Test]
    public async Task Should_record_failure_handling_time()
    {
        using TestingMetricListener metricsListener = await WhenMessagesHandled(() => new MyExceptionalMessage());
        metricsListener.AssertMetric(HandlingTimeMetricName, 5);
        AsserMandatoryTags(metricsListener, typeof(MyExceptionalMessage), typeof(MyExceptionalHandler));
        var exception = metricsListener.AssertTagKeyExists(HandlingTimeMetricName, "nservicebus.failure_type");
        Console.WriteLine(exception);
        Assert.AreEqual(typeof(Exception).FullName, exception);
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


    static void AsserMandatoryTags(TestingMetricListener metricsListener, Type expectedMessageType,
        Type expectedHandlerType)
    {
        var messageType = metricsListener.AssertTagKeyExists(HandlingTimeMetricName, "nservicebus.message_type");
        Assert.AreEqual(expectedMessageType.FullName, messageType);
        var handlerType =
            metricsListener.AssertTagKeyExists(HandlingTimeMetricName, "nservicebus.message_handler_type");
        Assert.AreEqual(expectedHandlerType.FullName, handlerType);
        var endpoint = metricsListener.AssertTagKeyExists(HandlingTimeMetricName, "nservicebus.queue");
        Assert.AreEqual(Conventions.EndpointNamingConvention(typeof(EndpointWithMetrics)), endpoint);
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