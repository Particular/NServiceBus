namespace NServiceBus.AcceptanceTests.Gateway
{
    using System;
    using Config;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_sending_a_message_to_another_site : NServiceBusAcceptanceTest
    {
        [Test, Ignore("It doesn't have any assertions!")]
        public void Should_be_able_to_reply_to_the_message()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<Headquarters>(b => b.Given(bus => bus.SendToSites(new[] { "SiteA" }, new MyRequest())))
                    .WithEndpoint<SiteA>()
                    .Done(c => c.GotResponseBack)
                    .Repeat(r => r.For(Transports.Default)
                    )
                    .Should(c =>
                        {
                            //Assert.AreEqual(CorrelationId,c.CorrelationIdReceived,"Correlation ids should match");
                        })
                    .Run();
        }

        public class Context : ScenarioContext
        {

            public bool GotResponseBack { get; set; }

        }

        public class Headquarters : EndpointConfigurationBuilder
        {
            public Headquarters()
            {
                EndpointSetup<DefaultServer>(c => c.RunGateway().UseInMemoryGatewayPersister())
                    .WithConfig<GatewayConfig>(c =>
                        {
                            c.Sites = new SiteCollection
                                {
                                    new SiteConfig
                                        {
                                            Key = "SiteA",
                                            Address = "http://localhost:25899/SiteA/",
                                            ChannelType = "http"
                                        }
                                };

                            c.Channels = new ChannelCollection
                                {
                                    new ChannelConfig
                                        {
                                             Address = "http://localhost:25899/Headquarters/",
                                            ChannelType = "http"
                                        }
                                };


                        });
            }

            public class MyResponseHandler : IHandleMessages<MyResponse>
            {
                public Context Context { get; set; }

                public IBus Bus { get; set; }

                public void Handle(MyResponse response)
                {
                    Context.GotResponseBack = true;
                }
            }
        }

        public class SiteA : EndpointConfigurationBuilder
        {
            public SiteA()
            {
                EndpointSetup<DefaultServer>(c => c.RunGateway().UseInMemoryGatewayPersister())
                        .WithConfig<GatewayConfig>(c =>
                        {
                            c.Channels = new ChannelCollection
                                {
                                    new ChannelConfig
                                        {
                                             Address = "http://localhost:25899/SiteA/",
                                            ChannelType = "http"
                                        }
                                };


                        });

            }

            public class MyRequestHandler : IHandleMessages<MyRequest>
            {
                public Context Context { get; set; }

                public IBus Bus { get; set; }

                public void Handle(MyRequest request)
                {
                    Bus.Reply(new MyResponse());
                }
            }
        }


        [Serializable]
        public class MyRequest : IMessage
        {
        }

        [Serializable]
        public class MyResponse : IMessage
        {
        }
    }
}
