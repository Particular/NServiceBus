namespace NServiceBus.AcceptanceTests.Core.Stopping
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using FakeTransport;
    using Logging;
    using NUnit.Framework;

    public class When_pump_throws_on_stop : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_log_exception()
        {
            var context = await Scenario.Define<ScenarioContext>()
                .WithEndpoint<EndpointThatThrowsOnPumpStop>()
                .Done(c => c.EndpointsStarted)
                .Run();

            var logItem = context.Logs.FirstOrDefault(item => item.Message.Contains("Receiver") && item.Level == LogLevel.Warn);
            Assert.IsNotNull(logItem);
            StringAssert.Contains("Receiver Main listening to queue PumpThrowsOnStop.EndpointThatThrowsOnPumpStop threw an exception on stopping. System.InvalidOperationException: ExceptionInPumpStop", logItem.Message);
        }

        public class EndpointThatThrowsOnPumpStop : EndpointConfigurationBuilder
        {
            public EndpointThatThrowsOnPumpStop()
            {
                EndpointSetup<DefaultServer>(builder =>
                {
                    builder.UseTransport<FakeTransport>()
                        .RaiseExceptionDuringPumpStop(new InvalidOperationException("ExceptionInPumpStop"));
                });
            }
        }
    }
}