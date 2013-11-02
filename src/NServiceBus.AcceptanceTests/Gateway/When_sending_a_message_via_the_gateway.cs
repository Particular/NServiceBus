namespace NServiceBus.AcceptanceTests.Gateway
{
    using System;
    using System.IO;
    using System.Net;
    using System.Web;
    using Config;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Gateway.Utils;
    using NUnit.Framework;
    using ScenarioDescriptors;
    
    public class When_sending_a_message_via_the_gateway : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_process_message()
        {
            Scenario.Define<Context>()
                .WithEndpoint<Headquarters>(b => b.When(bus =>
                {
                    var webRequest = (HttpWebRequest)WebRequest.Create("http://localhost:25898/Headquarters/");
                    webRequest.Method = "POST";
                    webRequest.ContentType = "text/xml; charset=utf-8";
                    webRequest.UserAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1)";

                    webRequest.Headers.Add("Content-Encoding", "utf-8");
                    webRequest.Headers.Add("NServiceBus.CallType", "Submit");
                    webRequest.Headers.Add("NServiceBus.AutoAck", "true");
                    webRequest.Headers.Add("MySpecialHeader", "MySpecialValue");
                    webRequest.Headers.Add("NServiceBus.Id", Guid.NewGuid().ToString("N"));

                    const string message = "<?xml version=\"1.0\" ?><Messages xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns=\"http://tempuri.net/NServiceBus.AcceptanceTests.Gateway\"><MyRequest></MyRequest></Messages>";
                    
                    using (var messagePayload = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(message)))
                    {
                        webRequest.Headers.Add(HttpRequestHeader.ContentMd5, HttpUtility.UrlEncode(Hasher.Hash(messagePayload)));
                        webRequest.ContentLength = messagePayload.Length;

                        using (var requestStream = webRequest.GetRequestStream())
                        {
                            messagePayload.CopyTo(requestStream);
                        }
                    }

                    while (true)
                    {
                        try
                        {
                            using (var myWebResponse = (HttpWebResponse) webRequest.GetResponse())
                            {
                                if (myWebResponse.StatusCode == HttpStatusCode.OK)
                                {
                                    break;
                                }
                            }
                        }
                        catch (WebException)
                        {
                        }
                    }
                }))
                .Done(c => c.GotMessage)
                .Repeat(r => r.For(Transports.Default))
                .Should(c =>
                {
                    Assert.IsTrue(c.GotMessage);
                    Assert.AreEqual("MySpecialValue", c.MySpecialHeader);
                })
                .Run();
        }

        public class Context : ScenarioContext
        {
            public bool GotMessage { get; set; }

            public string MySpecialHeader { get; set; }
        }

        public class Headquarters : EndpointConfigurationBuilder
        {
            public Headquarters()
            {
                EndpointSetup<DefaultServer>(c => c.RunGateway().UseInMemoryGatewayPersister())
                    .IncludeType<MyRequest>()
                    .WithConfig<GatewayConfig>(c =>
                    {
                        c.Channels = new ChannelCollection
                        {
                            new ChannelConfig
                            {
                                Address = "http://localhost:25898/Headquarters/",
                                ChannelType = "http"
                            }
                        };
                    });
            }

            public class MyResponseHandler : IHandleMessages<MyRequest>
            {
                public Context Context { get; set; }

                public IBus Bus { get; set; }

                public void Handle(MyRequest response)
                {
                    Context.GotMessage = true;
                    Context.MySpecialHeader = Bus.GetMessageHeader(response, "MySpecialHeader");
                }
            }
        }
    }

    [Serializable]
    public class MyRequest : IMessage
    {
    }
}
