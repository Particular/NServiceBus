namespace NServiceBus.AcceptanceTests.ScaleOut
{
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Settings;
    using NUnit.Framework;

    public class When_individualization_is_enabled : NServiceBusAcceptanceTest
    {
        const string discriminator = "something";

        [Test]
        public async Task Should_use_the_configured_differentiator()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<IndividualizedEndpoint>().Done(c => c.EndpointsStarted)
                    .Run();

            Assert.True(context.Address.Contains("something"), context.Address + " should contain the discriminator " + discriminator);

        }

        public class Context : ScenarioContext
        {
            public string Address { get; set; }
        }

        public class IndividualizedEndpoint : EndpointConfigurationBuilder
        {

            public IndividualizedEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.ScaleOut().InstanceDiscriminator(discriminator));
            }

            class AddressSpy : IWantToRunWhenBusStartsAndStops
            {
                public Context Context { get; set; }

                public ReadOnlySettings Settings { get; set; }

                public Task Start(IBusSession session)
                {
                    Context.Address = Settings.RootLogicalAddress().ToString();
                    return Task.FromResult(0);
                }

                public Task Stop(IBusSession session)
                {
                    return Task.FromResult(0);
                }
            }
        }
    }
}