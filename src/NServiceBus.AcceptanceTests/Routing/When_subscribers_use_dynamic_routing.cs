namespace NServiceBus.AcceptanceTests.Routing
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NServiceBus.Features;
    using NServiceBus.Routing;
    using NServiceBus.Settings;
    using NServiceBus.Unicast;
    using NServiceBus.Unicast.Subscriptions;
    using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;
    using NUnit.Framework;

    public class When_subscribers_use_dynamic_routing : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_route_events_dynamically()
        {
            Scenario.Define<Context>()
                .WithEndpoint<Publisher>(b => b.Given((bus, c) =>
                {
                    var unicastBus = (UnicastBus) bus;

                    var storage = unicastBus.Builder.Build<ISubscriptionStorage>();

                    storage.Subscribe(Address.Parse("LogicalAddress"), new[]
                    {
                        new MessageType(typeof(MyEvent))
                    });


                    //setup a fake route
                    unicastBus.Settings.Get<Dictionary<string, string>>("FakeRoutes")["LogicalAddress"] = unicastBus.Configure.LocalAddress.ToString();

                    bus.Publish(new MyEvent());
                }))
                .Done(c => c.GotTheMessage)
                .Repeat(c => c.For<AllTransportsWithMessageDrivenPubSub>())
                .Should(c => Assert.IsTrue(c.GotTheMessage))
                .Run();
        }

        public class Context : ScenarioContext
        {
            public bool GotTheMessage { get; set; }
        }

        public class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultServer>(c => c.UseDynamicRouting<FakeDynamicRouting>());
            }

            public class ResponseHandler : IHandleMessages<MyEvent>
            {
                public Context Context { get; set; }

                public void Handle(MyEvent message)
                {
                    Context.GotTheMessage = true;
                }
            }

            class FakeDynamicRouting:DynamicRoutingDefinition
            {
                protected override Type ProvidedByFeature()
                {
                    return typeof(FakeRoutingFeature);
                }
            }

            class FakeRoutingFeature:Feature
            {
                public FakeRoutingFeature()
                {
                    Defaults(s => s.Set("FakeRoutes", new Dictionary<string, string>()));
                }
                protected override void Setup(FeatureConfigurationContext context)
                {
                    context.Container.ConfigureComponent<FakeRouter>(DependencyLifecycle.SingleInstance);
                }
            }

            class FakeRouter:IProvideDynamicRouting
            {
                public ReadOnlySettings Settings { get; set; }

                public bool TryGetRouteAddress(string logicalEndpoint, out string address)
                {
                    var routes = Settings.Get<Dictionary<string, string>>("FakeRoutes");

                    return routes.TryGetValue(logicalEndpoint, out address);
                }
            }
        }

         [Serializable]
        public class MyEvent : IEvent
        {

        }

    }
}