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
        public async Task Should_not_throw_but_log_exception()
        {
            var context = await Scenario.Define<ScenarioContext>()
                .WithEndpoint<EndpointThatThrowsOnPumpStop>()
                .Done(c => c.EndpointsStarted)
                .Run();

            var logEntry = context.Logs.FirstOrDefault(l =>
                l.LoggerName == "NServiceBus.ReceiveComponent" && l.Level == LogLevel.Warn);

            Assert.IsNotNull(logEntry);
            StringAssert.StartsWith("Receiver Main threw an exception on stopping. System.InvalidOperationException: ExceptionInPumpStop", logEntry.Message);
        }

        public class EndpointThatThrowsOnPumpStop : EndpointConfigurationBuilder
        {
            public EndpointThatThrowsOnPumpStop()
            {
                EndpointSetup<DefaultServer>(builder =>
                {
                    var fakeTransport = new FakeTransport();
                    fakeTransport.RaiseExceptionOnReceiverStop(new InvalidOperationException("ExceptionInPumpStop"));
                    builder.UseTransport(fakeTransport);
                });
            }
        }
    }
}