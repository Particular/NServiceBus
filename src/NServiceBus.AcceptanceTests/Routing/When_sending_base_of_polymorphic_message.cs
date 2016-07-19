namespace NServiceBus.AcceptanceTests.Routing
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using Configuration.AdvanceExtensibility;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_sending_base_of_polymorphic_message : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_use_route_for_child_message()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<PolymorphicRoutingConfigSender>(e => e
                    .When(c => c.Send<IBaseMessage>(m => {})))
                .WithEndpoint<PolymorphicMessageReceiver>()
                .Done(c => true)
                .Run();

            Assert.That(context.ReceivedBaseMessage, Is.True);
            Assert.That(context.ReceivedChildMessage, Is.False);
        }

        [Test]
        public async Task Should_use_endpoint_mapping_for_child_message()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<PolymorphicEndpointMappingSender>(e => e
                    .When(c => c.Send<IBaseMessage>(m => { })))
                .WithEndpoint<PolymorphicMessageReceiver>()
                .Done(c => true)
                .Run();

            Assert.That(context.ReceivedBaseMessage, Is.True);
            Assert.That(context.ReceivedChildMessage, Is.False);
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
                EndpointSetup<DefaultServer>((c, r) =>
                {
                    var routingSettings = new TransportExtensions(c.GetSettings()).Routing();
                    routingSettings.RouteToEndpoint(typeof(IChildMessage), Conventions.EndpointNamingConvention(typeof(PolymorphicMessageReceiver)));
                });
            }
        }

        class PolymorphicEndpointMappingSender : EndpointConfigurationBuilder
        {
            public PolymorphicEndpointMappingSender()
            {
                // configure routing for the child message, no routing for the base message
                EndpointSetup<DefaultServer>().AddMapping<IChildMessage>(typeof(PolymorphicMessageReceiver));
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