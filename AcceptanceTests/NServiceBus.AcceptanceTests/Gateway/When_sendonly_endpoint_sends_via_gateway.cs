namespace NServiceBus.AcceptanceTests.Gateway
{
    using System;
    using Config;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_sendonly_endpoint_sends_via_gateway : NServiceBusAcceptanceTest
    {
        [Test, Ignore("Need help to get this working!")]
        public void Should_be_able_to_receive_message()
        {
            Scenario.Define<Context>()
                .WithEndpoint<SiteA>(
                    b => b.Given((bus, context) =>
                    {
                        Configure.Instance.ForInstallationOn<Installation.Environments.Windows>().Install();
                        bus.SendToSites(new[] {"SiteB"}, new MySiteARequest());
                    }))
                .WithEndpoint<SiteB>()
                .Done(c => c.GotMessage)
                .Repeat(r => r.For(Transports.Default))
                .Should(c => Assert.IsTrue(c.GotMessage))
                .Run();
        }

        public class Context : ScenarioContext
        {
            public bool GotMessage { get; set; }
        }

        public class SiteA : EndpointConfigurationBuilder
        {
            public SiteA()
            {
                EndpointSetup<DefaultServer>(c => c.RunGateway().UseInMemoryGatewayPersister().SendOnly())
                    .WithConfig<GatewayConfig>(c =>
                    {
                        c.Sites = new SiteCollection
                        {
                            new SiteConfig
                            {
                                Key = "SiteB",
                                Address = "http://localhost:25899/SiteB/",
                                ChannelType = "http"
                            }
                        };

                        c.Channels = new ChannelCollection
                        {
                            new ChannelConfig
                            {
                                Address = "http://localhost:25899/SiteA/",
                                ChannelType = "http",
                                Default = true
                            }
                        };
                    });
            }
        }

        public class SiteB : EndpointConfigurationBuilder
        {
            public SiteB()
            {
                EndpointSetup<DefaultServer>(c => c.RunGateway().UseInMemoryGatewayPersister())
                    .WithConfig<GatewayConfig>(c =>
                    {
                        c.Channels = new ChannelCollection
                        {
                            new ChannelConfig
                            {
                                Address = "http://localhost:25899/SiteB/",
                                ChannelType = "http",
                                Default = true
                            }
                        };
                    });

            }

            public class MyRequestHandler : IHandleMessages<MySiteARequest>
            {
                public Context Context { get; set; }

                public void Handle(MySiteARequest request)
                {
                    Context.GotMessage = true;
                }
            }
        }

        [Serializable]
        public class MySiteARequest : ICommand
        {
        }
    }
}
