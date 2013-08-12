namespace NServiceBus.AcceptanceTests.Gateway
{
    using System;
    using Config;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_doing_request_response_between_sites : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_be_able_to_reply_to_the_message()
        {
            Scenario.Define<Context>()
                .WithEndpoint<SiteA>(
                    b => b.Given((bus, context) =>
                        bus.SendToSites(new[] {"SiteB"}, new MyRequest())
                            .Register(result => context.GotCallback = true)))
                .WithEndpoint<SiteB>()
                .Done(c => c.GotResponseBack)
                .Repeat(r => r.For(Transports.Default))
                .Should(c =>
                {
                    Assert.IsTrue(c.GotResponseBack);
                    Assert.IsTrue(c.GotCallback);
                })
                .Run();
        }

        public class Context : ScenarioContext
        {
            public bool GotResponseBack { get; set; }
            public bool GotCallback { get; set; }
        }

        public class SiteA : EndpointConfigurationBuilder
        {
            public SiteA()
            {
                EndpointSetup<DefaultServer>(c => c.RunGateway().UseInMemoryGatewayPersister())
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

            public class MyResponseHandler : IHandleMessages<MyResponse>
            {
                public Context Context { get; set; }

                public void Handle(MyResponse response)
                {
                    Context.GotResponseBack = true;
                }
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

            public class MyRequestHandler : IHandleMessages<MyRequest>
            {
                public IBus Bus { get; set; }

                public void Handle(MyRequest request)
                {
                    Bus.Reply(new MyResponse());
                }
            }
        }

        [Serializable]
        public class MyRequest : ICommand
        {
        }

        [Serializable]
        public class MyResponse : IMessage
        {
        }
    }
}
