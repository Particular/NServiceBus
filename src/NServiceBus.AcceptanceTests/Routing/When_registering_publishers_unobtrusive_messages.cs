namespace NServiceBus.AcceptanceTests.Routing
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Configuration.AdvanceExtensibility;
    using CustomEventMessageNamespace;
    using EndpointTemplates;
    using NUnit.Framework;
    using AcceptanceTesting.Customization;
    using NServiceBus.Routing;
    using ScenarioDescriptors;
    using Settings;
    using Transport;

    public class When_registering_publishers_unobtrusive_messages
    {
        [Test]
        public Task Should_use_configured_routes_from_routing_api()
        {
            return Scenario.Define<Context>()
                .WithEndpoint<PublishingEndpoint>(e => e
                    .When(c => c.Subscribed, async s => await s.Publish(new SomeEvent())))
                .WithEndpoint<SubscribingEndpointUsingRoutingApi>()
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
        public Task Should_use_configured_routes_from_endpoint_mapping()
        {
            return Scenario.Define<Context>()
                .WithEndpoint<PublishingEndpoint>(e => e
                    .When(c => c.Subscribed, async s => await s.Publish(new SomeEvent())))
                .WithEndpoint<SubscribingEndpointUsingEndpointMappings>()
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

        public class PublishingEndpoint : EndpointConfigurationBuilder
        {
            public PublishingEndpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.OnEndpointSubscribed<Context>((e, ctx) => ctx.Subscribed = true);
                    c.Conventions().DefiningEventsAs(t => t == typeof(SomeEvent));
                });
            }
        }

        public class SubscribingEndpointUsingRoutingApi : EndpointConfigurationBuilder
        {
            public SubscribingEndpointUsingRoutingApi()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.Conventions().DefiningEventsAs(t => t == typeof(SomeEvent));

                    var routing = new RoutingSettings<MessageDrivenPubSubTransportDefinition>(c.GetSettings());
                    routing.RegisterPublisher(typeof(SomeEvent).Assembly, Conventions.EndpointNamingConvention(typeof(PublishingEndpoint)));
                }).IncludeType<SomeEvent>();
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

        public class SubscribingEndpointUsingEndpointMappings : EndpointConfigurationBuilder
        {
            public SubscribingEndpointUsingEndpointMappings()
            {
                EndpointSetup<DefaultServer>(c => c
                .Conventions().DefiningEventsAs(t => t == typeof(SomeEvent)))
                .AddMapping<SomeEvent>(typeof(PublishingEndpoint))
                .IncludeType<SomeEvent>();
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
    }
}

// custom namespace is required to avoid automatically loading the type by the testing framework
namespace CustomEventMessageNamespace
{
    public class SomeEvent
    {
    }
}