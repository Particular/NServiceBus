namespace NServiceBus.AcceptanceTests.Gateway
{
    using System;
    using Config;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;

    public class When_doing_request_return : NServiceBusAcceptanceTest
    {
        [Test]
        public void Callback_should_be_fired()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<SiteA>(
                    b => b.Given((bus, c) =>
                        bus.SendToSites(new[] { "SiteB" }, new MyRequest())
                            .Register(result => c.GotCallback = true)))
                .WithEndpoint<SiteB>()
                .Done(c => c.GotCallback)
                .Run(TimeSpan.FromSeconds(2000));

            Assert.IsTrue(context.GotCallback);
        }

        public class Context : ScenarioContext
        {
            public bool GotCallback { get; set; }
        }

        public class SiteA : EndpointConfigurationBuilder
        {
            public SiteA()
            {
                EndpointSetup<DefaultServer>(c => c.RunGateway()
                    .UseInMemoryGatewayPersister())
                    .AllowExceptions()
                    .WithConfig<GatewayConfig>(c =>
                    {
                        c.Sites = new SiteCollection
                        {
                            new SiteConfig
                            {
                                Key = "SiteB",
                                Address = "http://localhost:25699/SiteB/",
                                ChannelType = "http"
                            }
                        };

                        c.Channels = new ChannelCollection
                        {
                            new ChannelConfig
                            {
                                Address = "http://localhost:25699/SiteA/",
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
                    .AllowExceptions()
                    .WithConfig<GatewayConfig>(c =>
                    {
                        c.Channels = new ChannelCollection
                        {
                            new ChannelConfig
                            {
                                Address = "http://localhost:25699/SiteB/",
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
                    Bus.Return(1);
                }
            }
        }

        [Serializable]
        public class MyRequest : ICommand
        {
        }
    }
}
