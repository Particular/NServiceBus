namespace NServiceBus.Transport.Msmq.AcceptanceTests
{
    using AcceptanceTesting;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_no_explicit_error_queue_configured_for_sendonly_endpoint : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_start_endpoint()
        {
            Assert.DoesNotThrowAsync(async () =>
            {
                await Scenario.Define<Context>()
                    .WithEndpoint<EndpointWithNoErrorQConfig>()
                    .Run();
            });
        }

        public class EndpointWithNoErrorQConfig : EndpointConfigurationBuilder
        {
            public EndpointWithNoErrorQConfig()
            {
                EndpointSetup<PlainVanillaEndpoint>(endpointConfiguration =>
                {
                    endpointConfiguration.SendOnly();
                });
            }
        }

        public class PlainVanillaEndpoint : BasicServer
        {

            protected override void ApplyConfig(EndpointConfiguration configuration)
            {
                //no-op
            }
        }

        public class Context : ScenarioContext
        {
        }
    }
}