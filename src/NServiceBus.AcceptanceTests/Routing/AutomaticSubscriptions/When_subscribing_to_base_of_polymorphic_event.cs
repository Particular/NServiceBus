namespace NServiceBus.AcceptanceTests.Routing.AutomaticSubscriptions
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using Configuration.AdvanceExtensibility;
    using EndpointTemplates;
    using Features;
    using NServiceBus.Routing;
    using NUnit.Framework;

    public class When_subscribing_to_base_of_polymorphic_event
    {
        [Test]
        public async Task Should_use_child_route()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<PolymorphicRoutingConfigSubscriber>(e => e
                    .When(c => c.Subscribe<BaseEvent>()))
                .WithEndpoint<PolymorphicEventPublisher>()
                .Done(c => c.SubscribedEventTypes.Any())
                .Run();

            Assert.That(context.SubscribedEventTypes, Contains.Item(typeof(BaseEvent).FullName));
        }

        [Test]
        public async Task Should_use_child_endpoint_mapping()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<PolymorphicEndpointMappingSubscriber>(e => e
                    .When(c => c.Subscribe<BaseEvent>()))
                .WithEndpoint<PolymorphicEventPublisher>()
                .Done(c => c.SubscribedEventTypes.Any())
                .Run();

            Assert.That(context.SubscribedEventTypes, Contains.Item(typeof(BaseEvent).AssemblyQualifiedName));
        }

        class Context : ScenarioContext
        {
            public List<string> SubscribedEventTypes { get; } = new List<string>();
        }

        class PolymorphicRoutingConfigSubscriber : EndpointConfigurationBuilder
        {
            public PolymorphicRoutingConfigSubscriber()
            {
                // configure routing for the child message, no routing for the base message
                EndpointSetup<DefaultServer>(c =>
                {
                    c.DisableFeature<AutoSubscribe>();
                    c.GetSettings().GetOrCreate<UnicastRoutingTable>()
                        .RouteToEndpoint(typeof(ChildEvent), Conventions.EndpointNamingConvention(typeof(PolymorphicEventPublisher)));
                });
            }
        }

        class PolymorphicEndpointMappingSubscriber : EndpointConfigurationBuilder
        {
            public PolymorphicEndpointMappingSubscriber()
            {
                // configure routing for the child message, no routing for the base message
                EndpointSetup<DefaultServer>().AddMapping<ChildEvent>(typeof(PolymorphicEventPublisher));
            }
        }

        class PolymorphicEventPublisher : EndpointConfigurationBuilder
        {
            public PolymorphicEventPublisher()
            {
                EndpointSetup<DefaultServer>(c => c.OnEndpointSubscribed<Context>((subscripton, context) => context.SubscribedEventTypes.Add(subscripton.MessageType)));
            }
        }

        public class BaseEvent : IEvent
        {
        }

        public class ChildEvent : BaseEvent
        {
        }
    }
}