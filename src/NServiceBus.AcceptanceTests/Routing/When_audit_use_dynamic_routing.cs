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

    public class When_audit_use_dynamic_routing : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_route_audits_dynamically()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<EndpointThatAudits>(b => b.Given((bus, c) =>
                {
                    var unicastBus = (UnicastBus) bus;

                    //setup a fake route
                    unicastBus.Settings.Get<Dictionary<string, string>>("FakeRoutes")["boo"] = "audit_receiver";
                    unicastBus.Settings.Get<Dictionary<string, string>>("FakeRoutes")["foo"] = "forward_receiver";

                    bus.SendLocal(new MessageToGetAudited());
                }))
                .WithEndpoint<AuditReceiver>()
                .WithEndpoint<ForwardReceiver>()
                .Done(c => c.GotAuditMessage && c.GotForwardedMessage)
                .Run();

            Assert.IsTrue(context.GotAuditMessage);
            Assert.IsTrue(context.GotForwardedMessage);
        }

        public class Context : ScenarioContext
        {
            public bool GotAuditMessage { get; set; }
            public bool GotForwardedMessage { get; set; }
        }

        public class AuditReceiver : EndpointConfigurationBuilder
        {
            public AuditReceiver()
            {
                EndpointSetup<DefaultServer>(c => c.EndpointName("audit_receiver"));
            }

            public class MessageToGetAuditedHandler : IHandleMessages<MessageToGetAudited>
            {
                public Context Context { get; set; }

                public void Handle(MessageToGetAudited message)
                {
                    Context.GotAuditMessage = true;
                }
            }
        }

        public class ForwardReceiver : EndpointConfigurationBuilder
        {
            public ForwardReceiver()
            {
                EndpointSetup<DefaultServer>(c => c.EndpointName("forward_receiver"));
            }

            public class MessageToGetAuditedHandler : IHandleMessages<MessageToGetAudited>
            {
                public Context Context { get; set; }

                public void Handle(MessageToGetAudited message)
                {
                    Context.GotForwardedMessage = true;
                }
            }
        }

        public class EndpointThatAudits : EndpointConfigurationBuilder
        {
            public EndpointThatAudits()
            {
                EndpointSetup<DefaultServer>(c => c.UseDynamicRouting<FakeDynamicRouting>())
                    .WithConfig<AuditConfig>(c => c.QueueName = "boo")
                    .WithConfig<UnicastBusConfig>(c => c.ForwardReceivedMessagesTo = "foo");
            }

            public class MessageToGetAuditedHandler : IHandleMessages<MessageToGetAudited>
            {
                public void Handle(MessageToGetAudited message)
                {
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
        public class MessageToGetAudited : IMessage
        {
        }
    }
}