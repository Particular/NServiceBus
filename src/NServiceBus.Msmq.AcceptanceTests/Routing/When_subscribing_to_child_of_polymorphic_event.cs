namespace NServiceBus.AcceptanceTests.Routing
{
    using System;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using Features;
    using NUnit.Framework;

    public class When_subscribing_to_child_of_polymorphic_event : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_not_find_destination_from_routing()
        {
            var exception = Assert.ThrowsAsync<AggregateException>(async () => await Scenario.Define<ScenarioContext>()
                .WithEndpoint<PolymorphicRoutingConfigSubscriber>(e => e
                    .When(c => c.Subscribe<ChildEvent>()))
                .WithEndpoint<PolymorphicEventPublisher>()
                .Done(c => true)
                .Run());

            Assert.That(exception.InnerException.InnerException, Is.TypeOf<Exception>().With.Message.Contain("No publisher address could be found for message type NServiceBus.AcceptanceTests.Routing.When_subscribing_to_child_of_polymorphic_event+ChildEvent"));
        }

        [Test]
        public void Should_not_find_destination_from_endpoint_mappings()
        {
            var exception = Assert.ThrowsAsync<AggregateException>(async () => await Scenario.Define<ScenarioContext>()
                .WithEndpoint<PolymorphicEndpointMappingSubscriber>(e => e
                    .When(c => c.Subscribe<ChildEvent>()))
                .WithEndpoint<PolymorphicEventPublisher>()
                .Done(c => true)
                .Run());

            Assert.That(exception.InnerException.InnerException, Is.TypeOf<Exception>().With.Message.Contain("No publisher address could be found for message type NServiceBus.AcceptanceTests.Routing.When_subscribing_to_child_of_polymorphic_event+ChildEvent"));
        }

        class PolymorphicRoutingConfigSubscriber : EndpointConfigurationBuilder
        {
            public PolymorphicRoutingConfigSubscriber()
            {
                // configure routing for the child message, no routing for the base message
                EndpointSetup<DefaultServer>(c =>
                {
                    c.DisableFeature<AutoSubscribe>();
                    c.UseTransport<MsmqTransport>().Routing()
                        .RegisterPublisherForType(typeof(BaseEvent), Conventions.EndpointNamingConvention(typeof(PolymorphicEventPublisher)));
                });
            }
        }

        class PolymorphicEndpointMappingSubscriber : EndpointConfigurationBuilder
        {
            public PolymorphicEndpointMappingSubscriber()
            {
                // configure routing for the child message, no routing for the base message
                EndpointSetup<DefaultServer>().AddMapping<BaseEvent>(typeof(PolymorphicEventPublisher));
            }
        }

        class PolymorphicEventPublisher : EndpointConfigurationBuilder
        {
            public PolymorphicEventPublisher()
            {
                EndpointSetup<DefaultServer>();
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