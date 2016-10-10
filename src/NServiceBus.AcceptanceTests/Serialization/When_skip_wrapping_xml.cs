namespace NServiceBus.AcceptanceTests.Serialization
{
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using AcceptanceTesting;
    using EndpointTemplates;
    using MessageMutator;
    using NUnit.Framework;

    public class When_skip_wrapping_xml : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_wrap_xml_content()
        {
            XNamespace ns = "demoNamepsace";
            var xmlContent = new XDocument(new XElement(ns + "Document", new XElement(ns + "Value", "content")));

            var context = await Scenario.Define<Context>()
                .WithEndpoint<NonWrappingEndpoint>(e => e
                    .When(session => session.SendLocal(new MessageWithRawXml { Document = xmlContent })))
                .Done(c => c.MessageReceived)
                .Run();

            Assert.That(context.XmlPropertyValue.ToString(), Is.EqualTo(xmlContent.ToString()));
            Assert.That(context.XmlMessage.Root.Name.LocalName, Is.EqualTo(nameof(MessageWithRawXml)));
            Assert.That(context.XmlMessage.Root.Elements().Single().ToString(), Is.EqualTo(xmlContent.ToString()));
        }

        class Context : ScenarioContext
        {
            public bool MessageReceived { get; set; }

            public XDocument XmlMessage { get; set; }

            public XDocument XmlPropertyValue { get; set; }
        }

        class NonWrappingEndpoint : EndpointConfigurationBuilder
        {
            public NonWrappingEndpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.UseSerialization<XmlSerializer>().DontWrapRawXml();
                    c.RegisterComponents(r => r.ConfigureComponent<IncomingMutator>(DependencyLifecycle.SingleInstance));
                });
            }

            class RawXmlMessageHandler : IHandleMessages<MessageWithRawXml>
            {
                public RawXmlMessageHandler(Context scenarioContext)
                {
                    this.scenarioContext = scenarioContext;
                }

                public Task Handle(MessageWithRawXml messageWithRawXml, IMessageHandlerContext context)
                {
                    scenarioContext.MessageReceived = true;
                    scenarioContext.XmlPropertyValue = messageWithRawXml.Document;

                    return Task.FromResult(0);
                }

                Context scenarioContext;
            }

            public class IncomingMutator : IMutateIncomingTransportMessages
            {
                public IncomingMutator(Context scenarioContext)
                {
                    this.scenarioContext = scenarioContext;
                }

                public Task MutateIncoming(MutateIncomingTransportMessageContext context)
                {
                    scenarioContext.XmlMessage = XDocument.Parse(Encoding.UTF8.GetString(context.Body));

                    return Task.FromResult(0);
                }

                Context scenarioContext;
            }
        }

        public class MessageWithRawXml : ICommand
        {
            public XDocument Document { get; set; }
        }
    }
}