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
        static readonly byte[] PayloadToSend = new byte[1024 * 1024 * 10];

        [Test]
        public void Should_be_able_to_reply_to_the_message()
        {
            Scenario.Define<Context>()
                .WithEndpoint<SiteA>(
                    b => b.Given((bus, context) =>
                        bus.SendToSites(new[] { "SiteB" }, new MyRequest() { Payload = new DataBusProperty<byte[]>(PayloadToSend) })
                            .Register(result => context.GotCallback = true)))
                .WithEndpoint<SiteB>()
                .Done(c => c.GotResponseBack)
                .Repeat(r => r.For(Transports.Default))
                .Should(c =>
                {
                    Assert.IsTrue(c.GotResponseBack);
                    Assert.IsTrue(c.GotCallback);

                    // Assert that we are able to use the DataBus to send to SiteB
                    Assert.AreEqual(PayloadToSend, c.SiteBReceivedPayload,
                        "The large payload should be marshalled correctly using the databus");

                    // Assert that we are able to use the DataBus to send a response back to SiteA
                    Assert.AreEqual(PayloadToSend, c.SiteAReceivedPayloadInResponse,
                        "The large payload should be marshalled correctly using the databus");

                    // Assert that when SiteB received the request, the headers for the Originating Site is SIteA
                    Assert.AreEqual(@"http,http://localhost:25899/SiteA/NumberOfWorkerThreads=1Default=True", c.OriginatingSiteForRequest);
                    
                    // Assert that when SiteA received the response, the headers for the Originating Site is SIteB
                    Assert.AreEqual(@"http,http://localhost:25899/SiteB/NumberOfWorkerThreads=1Default=True", c.OriginatingSiteForResponse);
                })
                .Run();
        }

        public class Context : ScenarioContext
        {
            public bool GotResponseBack { get; set; }
            public bool GotCallback { get; set; }
            public byte[] SiteBReceivedPayload { get; set; }
            public byte[] SiteAReceivedPayloadInResponse { get; set; }
            public string OriginatingSiteForRequest { get; set; }
            public string OriginatingSiteForResponse { get; set; }
        }

        public class SiteA : EndpointConfigurationBuilder
        {
            public SiteA()
            {
                EndpointSetup<DefaultServer>(c => c.RunGateway()
                    .UseInMemoryGatewayPersister()
                    .FileShareDataBus(@".\databus\sender"))
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
                public IBus Bus { get; set; }

                public void Handle(MyResponse response)
                {
                    Context.GotResponseBack = true;
                    Context.SiteAReceivedPayloadInResponse = response.OriginalPayload.Value;
                    
                    // Inspect the headers to find the originating site address 
                    Context.OriginatingSiteForResponse = Bus.CurrentMessageContext.Headers[Headers.OriginatingSite];
                }
            }
        }

        public class SiteB : EndpointConfigurationBuilder
        {
            public SiteB()
            {
                EndpointSetup<DefaultServer>(c => c.RunGateway().UseInMemoryGatewayPersister()
                    .FileShareDataBus(@".\databus\sender"))
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
                public Context Context { get; set; }

                public void Handle(MyRequest request)
                {
                    Context.SiteBReceivedPayload = request.Payload.Value;
                    Bus.Reply(new MyResponse(){OriginalPayload = request.Payload});

                    // Inspect the headers to find the originating site address
                    Context.OriginatingSiteForRequest = Bus.CurrentMessageContext.Headers[Headers.OriginatingSite];
                }
            }
        }

        [Serializable]
        public class MyRequest : ICommand
        {
            public DataBusProperty<byte[]> Payload { get; set; }
        }

        [Serializable]
        public class MyResponse : IMessage
        {
            public DataBusProperty<byte[]> OriginalPayload { get; set; }
        }
    }
}
