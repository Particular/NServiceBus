namespace NServiceBus.AcceptanceTests.Routing
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using Configuration.AdvanceExtensibility;
    using EndpointTemplates;
    using NServiceBus.Routing;
    using NUnit.Framework;

    public class When_sending_child_of_polymorphic_message : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_not_find_destination_from_routing()
        {
            var exception = Assert.ThrowsAsync<AggregateException>(async() => await Scenario.Define<Context>()
                .WithEndpoint<PolymorphicRoutingConfigSender>(e => e
                    .When(c => c.Send<IChildMessage>(m => { })))
                .WithEndpoint<PolymorphicMessageReceiver>()
                .Done(c => true)
                .Run());

            Assert.That(exception.InnerException.InnerException, Is.TypeOf<Exception>().With.Message.Contain("No destination specified for message: NServiceBus.AcceptanceTests.Routing.When_sending_child_of_polymorphic_message+IChildMessage"));
        }

        [Test]
        public void Should_not_find_destination_from_endpoint_mappings()
        {
            var exception = Assert.ThrowsAsync<AggregateException>(async () => await Scenario.Define<Context>()
                .WithEndpoint<PolymorphicEndpointMappingSender>(e => e
                    .When(c => c.Send<IChildMessage>(m => { })))
                .WithEndpoint<PolymorphicMessageReceiver>()
                .Done(c => true)
                .Run());

            Assert.That(exception.InnerException.InnerException, Is.TypeOf<Exception>().With.Message.Contain("No destination specified for message: NServiceBus.AcceptanceTests.Routing.When_sending_child_of_polymorphic_message+IChildMessage"));
        }

        class Context : ScenarioContext
        {
            public bool ReceivedBaseMessage { get; set; }
            public bool ReceivedChildMessage { get; set; }
        }

        class PolymorphicRoutingConfigSender : EndpointConfigurationBuilder
        {
            public PolymorphicRoutingConfigSender()
            {
                // configure routing for the child message, no routing for the base message
                EndpointSetup<DefaultServer>(c => c.GetSettings().GetOrCreate<UnicastRoutingTable>()
                    .RouteToEndpoint(typeof(IBaseMessage), Conventions.EndpointNamingConvention(typeof(PolymorphicMessageReceiver))));
            }
        }

        class PolymorphicEndpointMappingSender : EndpointConfigurationBuilder
        {
            public PolymorphicEndpointMappingSender()
            {
                // configure routing for the child message, no routing for the base message
                EndpointSetup<DefaultServer>().AddMapping<IBaseMessage>(typeof(PolymorphicMessageReceiver));
            }
        }

        class PolymorphicMessageReceiver : EndpointConfigurationBuilder
        {
            public PolymorphicMessageReceiver()
            {
                EndpointSetup<DefaultServer>();
            }

            class BaseMessageHandler : IHandleMessages<IBaseMessage>, IHandleMessages<IChildMessage>
            {
                Context testContext;

                public BaseMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(IBaseMessage message, IMessageHandlerContext context)
                {
                    testContext.ReceivedBaseMessage = true;
                    return Task.FromResult(0);
                }

                public Task Handle(IChildMessage message, IMessageHandlerContext context)
                {
                    testContext.ReceivedChildMessage = true;
                    return Task.FromResult(0);
                }
            }
        }

        public interface IBaseMessage : ICommand
        {
        }

        public interface IChildMessage : IBaseMessage
        {
        }
    }
}