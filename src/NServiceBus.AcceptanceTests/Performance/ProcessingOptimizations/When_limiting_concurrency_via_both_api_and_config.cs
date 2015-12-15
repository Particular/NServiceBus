namespace NServiceBus.AcceptanceTests.Config
{
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.FakeTransport;
    using NServiceBus.Config;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_limiting_concurrency_via_both_api_and_config : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_throw()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<ThrottledEndpoint>(b => b.CustomConfig(c => c.LimitMessageProcessingConcurrencyTo(10)))
                .Done(c => c.EndpointsStarted)
                .Repeat(r => r.For(Transports.AllAvailable.SingleOrDefault(t => t.Key == "FakeTransport")))
                .Should(c => Assert.True(c.Exceptions.First().Message.Contains("specified both via API and configuration")))
                .Run();
        }

        public class Context : ScenarioContext
        {
        }

        class ThrottledEndpoint : EndpointConfigurationBuilder
        {
            public ThrottledEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.UseTransport<FakeTransport>())
                    .WithConfig<TransportConfig>(c => c.MaximumConcurrencyLevel = 8);
            }
        }
    }
}