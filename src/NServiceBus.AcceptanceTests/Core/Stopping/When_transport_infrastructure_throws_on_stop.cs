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

            var logItem = context.Logs.FirstOrDefault(item => item.Message.Contains("Shutdown of the transport") && item.Level == LogLevel.Error);
            Assert.IsNotNull(logItem);
            StringAssert.Contains("Shutdown of the transport infrastructure failed. System.InvalidOperationException: ExceptionInInfrastructureStop", logItem.Message);
        }

        public class EndpointThatThrowsOnInfrastructureStop : EndpointConfigurationBuilder
        {
            public EndpointThatThrowsOnInfrastructureStop()
            {
                EndpointSetup<DefaultServer>(builder =>
                {
                    var fakeTransport = new FakeTransport();
                    fakeTransport.RaiseExceptionOnTransportDispose(new InvalidOperationException("ExceptionInInfrastructureStop"));
                    builder.UseTransport(fakeTransport);
                });
            }
        }
    }
}
