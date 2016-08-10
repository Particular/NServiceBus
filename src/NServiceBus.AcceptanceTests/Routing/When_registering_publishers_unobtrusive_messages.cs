namespace NServiceBus.AcceptanceTests.Routing
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Configuration.AdvanceExtensibility;
    using EndpointTemplates;
    using NUnit.Framework;
    using AcceptanceTesting.Customization;
    using NServiceBus.Routing;
    using ScenarioDescriptors;
    using Settings;
    using Transport;

    public class When_registering_publishers_unobtrusive_messages : NServiceBusAcceptanceTest
    {
        [Test]
        public Task Should_use_routes_from_routing_api()
        {
            return Scenario.Define<Context>()
                .WithEndpoint<Publisher>(e => e
                    .When(c => c.Subscribed, s => s.Publish(new SomeEvent())))
                .WithEndpoint<SubscriberUsingRoutingApi>()
                .Done(c => c.ReceivedMessage)
                .Repeat(r => r.For<AllTransportsWithMessageDrivenPubSub>())
                .Should(context =>
                {
                    Assert.That(context.Subscribed, Is.True);
                    Assert.That(context.ReceivedMessage, Is.True);
                })
                .Run();
        }

        [Test]
        public Task Should_use_routes_from_endpoint_mapping()
        {
            return Scenario.Define<Context>()
                .WithEndpoint<Publisher>(e => e
                    .When(c => c.Subscribed, s => s.Publish(new SomeEvent())))
                .WithEndpoint<SubscriberUsingEndpointMappings>()
                .Done(c => c.ReceivedMessage)
                .Repeat(r => r.For<AllTransportsWithMessageDrivenPubSub>())
                .Should(context =>
                {
                    Assert.That(context.Subscribed, Is.True);
                    Assert.That(context.ReceivedMessage, Is.True);
                })
                .Run();
        }

        public class Context : ScenarioContext
        {
            public bool Subscribed { get; set; }
            public bool ReceivedMessage { get; set; }
        }

        public class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.OnEndpointSubscribed<Context>((e, ctx) => ctx.Subscribed = true);
                    c.Conventions().DefiningEventsAs(t => t == typeof(SomeEvent));
                }).ExcludeType<SomeEvent>();
            }
        }

        public class SubscriberUsingRoutingApi : EndpointConfigurationBuilder
        {
            public SubscriberUsingRoutingApi()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.Conventions().DefiningEventsAs(t => t == typeof(SomeEvent));

                    var routing = new RoutingSettings<MessageDrivenPubSubTransportDefinition>(c.GetSettings());
                    routing.RegisterPublisher(typeof(SomeEvent).Assembly, Conventions.EndpointNamingConvention(typeof(Publisher)));
                });
            }

            public class EventHandler : IHandleMessages<SomeEvent>
            {
                Context testContext;

                public EventHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(SomeEvent message, IMessageHandlerContext context)
                {
                    testContext.ReceivedMessage = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class SubscriberUsingEndpointMappings : EndpointConfigurationBuilder
        {
            public SubscriberUsingEndpointMappings()
            {
                EndpointSetup<DefaultServer>(c => c
                .Conventions().DefiningEventsAs(t => t == typeof(SomeEvent)))
                .AddMapping<SomeEvent>(typeof(Publisher));
            }

            public class EventHandler : IHandleMessages<SomeEvent>
            {
                Context testContext;

                public EventHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(SomeEvent message, IMessageHandlerContext context)
                {
                    testContext.ReceivedMessage = true;
                    return Task.FromResult(0);
                }
            }
        }

        class MessageDrivenPubSubTransportDefinition : TransportDefinition, IMessageDrivenSubscriptionTransport
        {
            public override string ExampleConnectionStringForErrorMessage { get; }

            public override TransportInfrastructure Initialize(SettingsHolder settings, string connectionString)
            {
                throw new System.NotImplementedException();
            }
        }

        public class SomeEvent
        {
        }
    }
}