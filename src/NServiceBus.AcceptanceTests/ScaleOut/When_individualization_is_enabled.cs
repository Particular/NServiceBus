namespace NServiceBus.AcceptanceTests.ScaleOut
{
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_individualization_is_enabled : NServiceBusAcceptanceTest
    {
        const string discriminator = "-something";

        
        [Test]
        public void Should_use_the_configured_differentiator()
        {
            var context = Scenario.Define<Context>()
                    .WithEndpoint<IndividualizedEndpoint>().Done(c =>c.EndpointsStarted)
                    .Run();

           
            Assert.True(context.Address.Contains("-something"),context.Address + " should contain the discriminator " + discriminator);

        }

        public class Context : ScenarioContext
        {
            public string Address { get; set; }
        }

        public class IndividualizedEndpoint : EndpointConfigurationBuilder
        {
       
            public IndividualizedEndpoint()
            {
                EndpointSetup<DefaultServer>(c=>c.ScaleOut().UniqueQueuePerEndpointInstance(discriminator));
            }

            class AddressSpy : IWantToRunWhenBusStartsAndStops
            {
                public Context Context { get; set; }

                public Configure Configure { get; set; }

                public void Start()
                {
                    Context.Address = Configure.LocalAddress.ToString();
                }

                public void Stop()
                {
                }
            }
        }
    }
}