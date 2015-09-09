namespace NServiceBus.AcceptanceTests.Config
{
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.FakeTransport;
    using NUnit.Framework;

    public class When_using_concurrency_limit : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_pass_it_to_the_transport()
        {
            var context = await Scenario.Define<FakeTransportContext>()
                    .WithEndpoint<ThrottledEndpoint>(b => b.CustomConfig(c => c.LimitMessageProcessingConcurrencyTo(10)))
                    .Done(c => c.EndpointsStarted)
                    .Run();

            Assert.AreEqual(10, context.PushRuntimeSettings.MaxConcurrency);
        }

        class ThrottledEndpoint : EndpointConfigurationBuilder
        {
            public ThrottledEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.UseTransport<FakeTransport>()).IncludeType<FakeTransportConfigurator>();
            }
        }
    }
}