﻿namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry;

using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using NUnit.Framework;
using Conventions = AcceptanceTesting.Customization.Conventions;

public class WhenIncomingMessageHandledSuccessfully : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_record_handling_time()
    {
        using var metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();

        _ = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithMetrics>(b =>
                b.When(async (session, _) =>
                {
                    for (var x = 0; x < 5; x++)
                    {
                        await session.SendLocal(new MyMessage());
                    }
                }))
            .Done(c => c.TotalHandledMessages == 5)
            .Run();

        string handlingTime = "nservicebus.messaging.handling_time";
        metricsListener.AssertMetric(handlingTime, 5);
        var messageType = metricsListener.AssertTagKeyExists(handlingTime, "nservicebus.message_type");
        Assert.AreEqual(typeof(MyMessage).FullName, messageType);
        var handlerType = metricsListener.AssertTagKeyExists(handlingTime, "nservicebus.message_handler_type");
        Assert.AreEqual(typeof(MyMessageHandler).FullName, handlerType);
        var endpoint = metricsListener.AssertTagKeyExists(handlingTime, "nservicebus.queue");
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

    class MyMessage : IMessage
    {
    }
}