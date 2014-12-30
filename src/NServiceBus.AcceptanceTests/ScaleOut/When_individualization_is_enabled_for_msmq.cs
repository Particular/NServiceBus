namespace NServiceBus.AcceptanceTests.ScaleOut
{
    using System.Linq;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NServiceBus.Settings;
    using NUnit.Framework;

    public class When_individualization_is_enabled_for_msmq : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_be_a_no_op_discriminator()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<IndividualizedEndpoint>().Done(c =>c.EndpointsStarted)
                    .Repeat(r => r.For<MsmqOnly>())
                    .Should(c=>Assert.AreEqual(c.EndpointName,c.Address.Split('@').First()))
                    .Run();
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
                EndpointSetup<DefaultServer>(c=>c.ScaleOut().UniqueQueuePerEndpointInstance());
            }

            class AddressSpy : IWantToRunWhenBusStartsAndStops
            {
                public Context Context { get; set; }

                public Configure Configure { get; set; }

                public ReadOnlySettings ReadOnlySettings { get; set; }

                public void Start()
                {
                    Context.Address = Configure.LocalAddress.ToString();
                    Context.EndpointName = ReadOnlySettings.EndpointName();
                }

                public void Stop()
                {
                }
            }
        }
    }
}