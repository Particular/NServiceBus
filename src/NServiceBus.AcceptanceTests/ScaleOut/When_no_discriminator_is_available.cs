namespace NServiceBus.AcceptanceTests.ScaleOut
{
    using System;
    using System.Linq;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Features;
    using NServiceBus.Transports;
    using NUnit.Framework;

    public class When_no_discriminator_is_available : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_blow_up()
        {
            var ex = Assert.Throws<AggregateException>(()=> Scenario.Define<Context>()
                    .WithEndpoint<IndividualizedEndpoint>().Done(c =>c.EndpointsStarted)
                    .AllowExceptions()
                    .Run());

            var configEx = ex.InnerExceptions.First()
                .InnerException;

            Assert.True(configEx.Message.StartsWith("No endpoint instance discriminator found"));

        }

        public class Context : ScenarioContext
        {
        }

        public class IndividualizedEndpoint : EndpointConfigurationBuilder
        {
       
            public IndividualizedEndpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.ScaleOut().UniqueQueuePerEndpointInstance();
                    c.UseTransport<TransportThatDoesntSetADefaultDiscriminator>();
                });
            }
        }

        public class TransportThatDoesntSetADefaultDiscriminator:TransportDefinition
        {
            protected override void Configure(BusConfiguration config)
            {
                config.EnableFeature<TransportThatDoesntSetADefaultDiscriminatorConfigurator>();
            }
        }

            public class TransportThatDoesntSetADefaultDiscriminatorConfigurator : ConfigureTransport
            {
                protected override void Configure(FeatureConfigurationContext context, string connectionString)
                {
                    
                }

                protected override string ExampleConnectionStringForErrorMessage
                {
                    get { return ""; }
                }
            }
    }

    
}