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

    public class When_transport_infrastructure_throws_on_stop : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_log_exception()
        {
            var context = await Scenario.Define<ScenarioContext>()
                .WithEndpoint<EndpointThatThrowsOnInfrastructureStop>()
                .Done(c => c.EndpointsStarted)
                .Run();

            var logItem = context.Logs.FirstOrDefault(item => item.Message.Contains("shutdown") && item.Level == LogLevel.Warn);
            Assert.IsNotNull(logItem);
            StringAssert.Contains("Exception occurred during shutdown of the transport. System.InvalidOperationException: ExceptionInInfrastructureStop", logItem.Message);
        }

        public class EndpointThatThrowsOnInfrastructureStop : EndpointConfigurationBuilder
        {
            public EndpointThatThrowsOnInfrastructureStop()
            {
                EndpointSetup<DefaultServer>(builder =>
                {
                    builder.UseTransport(new FakeTransport()
                    {
                        ThrowOnInfrastructureStop = true,
                        ExceptionToThrow = new InvalidOperationException("ExceptionInInfrastructureStop")
                    });
                });
            }
        }
    }
}