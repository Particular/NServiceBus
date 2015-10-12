namespace NServiceBus.AcceptanceTests.ScaleOut
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Features;
    using NServiceBus.Settings;
    using NServiceBus.Transports;
    using NUnit.Framework;

    public class When_no_discriminator_is_available : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_blow_up()
        {
            AggregateException ex = null;
            try
            {
                await Scenario.Define<Context>()
                    .WithEndpoint<IndividualizedEndpoint>().Done(c => c.EndpointsStarted)
                    .AllowExceptions()
                    .Run();
            }
            catch (AggregateException e)
            {
                ex = e;
            }

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
                    c.UseTransport<TransportThatDoesNotSetADefaultDiscriminator>();
                })
                    .IncludeType<TransportThatDoesNotSetADefaultDiscriminatorConfigurator>();
            }
        }

        public class TransportThatDoesNotSetADefaultDiscriminator : TransportDefinition
        {
            public override IEnumerable<Type> GetSupportedDeliveryConstraints()
            {
                return new List<Type>();
            }

            public override TransactionSupport GetTransactionSupport()
            {
                return TransactionSupport.SingleQueue;
            }

            public override IManageSubscriptions GetSubscriptionManager()
            {
                throw new NotImplementedException();
            }

            public override string GetDiscriminatorForThisEndpointInstance()
            {
                return null;
            }

            public override string ToTransportAddress(LogicalAddress logicalAddress)
            {
                throw new NotImplementedException();
            }

            public override OutboundRoutingPolicy GetOutboundRoutingPolicy(ReadOnlySettings settings)
            {
                return new OutboundRoutingPolicy(OutboundRoutingType.DirectSend, OutboundRoutingType.DirectSend, OutboundRoutingType.DirectSend);
            }
        }

        public class TransportThatDoesNotSetADefaultDiscriminatorConfigurator : ConfigureTransport
        {
            protected override string ExampleConnectionStringForErrorMessage => "";

            protected override void Configure(FeatureConfigurationContext context, string connectionString)
            {
            }
        }
    }
}