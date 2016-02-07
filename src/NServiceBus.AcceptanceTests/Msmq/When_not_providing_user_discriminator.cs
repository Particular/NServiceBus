namespace NServiceBus.AcceptanceTests.Msmq
{
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Settings;
    using NServiceBus.Support;
    using NServiceBus.Transports;
    using NUnit.Framework;

    public class When_not_providing_user_discriminator : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_use_only_the_machine_name()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<IndividualizedEndpoint>().Done(c => c.EndpointsStarted)
                .Run();

            Assert.AreEqual(context.EndpointName + "@" + RuntimeEnvironment.MachineName, context.Address);
        }

        public class Context : ScenarioContext
        {
            public string Address { get; set; }
            public string EndpointName { get; set; }
        }

        public class IndividualizedEndpoint : EndpointConfigurationBuilder
        {
            public IndividualizedEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            class AddressSpy : IWantToRunWhenBusStartsAndStops
            {
                public Context Context { get; set; }

                public ReadOnlySettings ReadOnlySettings { get; set; }

                public Task Start(IMessageSession session)
                {
                    Context.Address = ReadOnlySettings.Get<TransportDefinition>().ToTransportAddress(ReadOnlySettings.RootLogicalAddress());
                    Context.EndpointName = ReadOnlySettings.EndpointName().ToString();
                    return Task.FromResult(0);
                }

                public Task Stop(IMessageSession session)
                {
                    return Task.FromResult(0);
                }
            }
        }
    }
}