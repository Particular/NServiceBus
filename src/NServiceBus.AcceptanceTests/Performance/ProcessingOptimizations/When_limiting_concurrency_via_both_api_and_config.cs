namespace NServiceBus.AcceptanceTests.Config
{
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.FakeTransport;
    using NServiceBus.Config;
    using NUnit.Framework;

    public class When_limiting_concurrency_via_both_api_and_config : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_throw()
        {
            var context = await Scenario.Define<FakeTransportContext>()
                .WithEndpoint<ThrottledEndpoint>(b => b.CustomConfig(c => c.LimitMessageProcessingConcurrencyTo(10)))
                .AllowExceptions()
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.True(context.Exceptions.First().Message.Contains("specified both via API and configuration"));
        }

        class ThrottledEndpoint : EndpointConfigurationBuilder
        {
            public ThrottledEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.UseTransport<FakeTransport>())
                    .IncludeType<FakeTransportConfigurator>()
                    .WithConfig<TransportConfig>(c => c.MaximumConcurrencyLevel = 8);

            }
        }
    }
}