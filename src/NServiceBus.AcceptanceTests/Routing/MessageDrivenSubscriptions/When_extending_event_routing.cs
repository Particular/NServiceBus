namespace NServiceBus.AcceptanceTests.Routing.MessageDrivenSubscriptions
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using Features;
    using NServiceBus.Routing.MessageDrivenSubscriptions;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_extending_event_routing : NServiceBusAcceptanceTest
    {
        static string PublisherEndpoint => Conventions.EndpointNamingConvention(typeof(Publisher));

        [Test]
        public async Task Should_route_events_correctly()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<Publisher>(b =>
                    b.When(c => c.Subscribed, session => session.Publish<MyEvent>())
                )
                .WithEndpoint<Subscriber>(b => b.When(async (session, context) => { await session.Subscribe<MyEvent>(); }))
                .Done(c => c.MessageDelivered)
                .Repeat(r => r.For<AllTransportsWithMessageDrivenPubSub>())
                .Should(c => Assert.True(c.MessageDelivered))
                .Run();
        }

        public class Context : ScenarioContext
        {
            public bool MessageDelivered { get; set; }
            public bool Subscribed { get; set; }
        }

        public class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultPublisher>(b => b.OnEndpointSubscribed<Context>((s, context) => { context.Subscribed = true; }));
            }
        }

        public class Subscriber : EndpointConfigurationBuilder
        {
            public Subscriber()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.DisableFeature<AutoSubscribe>();
                    c.EnableFeature<CustomPublisherRoutes>();
                });
            }

            class CustomPublisherRoutes : Feature
            {
                protected override void Setup(FeatureConfigurationContext context)
                {
                    context.Routing.Publishers.AddOrReplacePublishers("CustomRoutingFeature", new List<PublisherTableEntry>
                    {
                        new PublisherTableEntry(typeof(MyEvent), PublisherAddress.CreateFromEndpointName(PublisherEndpoint))
                    });
                }
            }

            public class MyEventHandler : IHandleMessages<MyEvent>
            {
                public Context Context { get; set; }

                public Task Handle(MyEvent evnt, IMessageHandlerContext context)
                {
                    Context.MessageDelivered = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class MyEvent : IEvent
        {
        }
    }
}