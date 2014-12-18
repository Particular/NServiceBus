namespace NServiceBus.AcceptanceTests.Routing
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NServiceBus.Features;
    using NServiceBus.Routing;
    using NServiceBus.Settings;
    using NServiceBus.Unicast;
    using NUnit.Framework;

    public class When_errors_use_dynamic_routing : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_route_errors_dynamically()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<EndpointThatFails>(b => b.Given((bus, c) =>
                {
                    var unicastBus = (UnicastBus) bus;

                    //setup a fake route
                    unicastBus.Settings.Get<Dictionary<string, string>>("FakeRoutes")["boo"] = "error_receiver";

                    bus.SendLocal(new MessageThatThrows());
                }))
                .AllowExceptions(e => e.Message.Contains("MessageThatThrowsHandler Exception"))
                .WithEndpoint<ErrorReceiver>()
                .Done(c => c.GotTheMessage)
                .Run();

            Assert.IsTrue(context.GotTheMessage);
        }

        public class Context : ScenarioContext
        {
            public bool GotTheMessage { get; set; }
        }

        public class ErrorReceiver : EndpointConfigurationBuilder
        {
            public ErrorReceiver()
            {
                EndpointSetup<DefaultServer>(c => c.EndpointName("error_receiver"));
            }

            public class MessageThatThrowsHandler : IHandleMessages<MessageThatThrows>
            {
                public Context Context { get; set; }

                public void Handle(MessageThatThrows message)
                {
                    Context.GotTheMessage = true;
                }
            }
        }

        public class EndpointThatFails : EndpointConfigurationBuilder
        {
            public EndpointThatFails()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.DisableFeature<SecondLevelRetries>();
                    c.UseDynamicRouting<FakeDynamicRouting>();
                })
                    .WithConfig<MessageForwardingInCaseOfFaultConfig>(c => c.ErrorQueue = "boo");
            }

            public class MessageThatThrowsHandler : IHandleMessages<MessageThatThrows>
            {
                public void Handle(MessageThatThrows message)
                {
                    throw new Exception("MessageThatThrowsHandler Exception");
                }
            }

            class FakeDynamicRouting : DynamicRoutingDefinition
            {
                protected override Type ProvidedByFeature()
                {
                    return typeof(FakeRoutingFeature);
                }
            }

            class FakeRouter : IProvideDynamicRouting
            {
                public ReadOnlySettings Settings { get; set; }

                public bool TryGetRouteAddress(string logicalEndpoint, out string address)
                {
                    var routes = Settings.Get<Dictionary<string, string>>("FakeRoutes");

                    return routes.TryGetValue(logicalEndpoint, out address);
                }
            }

            class FakeRoutingFeature : Feature
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
        }

        [Serializable]
        public class MessageThatThrows : IMessage
        {
        }
    }
}