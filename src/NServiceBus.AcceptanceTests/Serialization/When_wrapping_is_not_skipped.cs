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

    public class When_wrapping_is_not_skipped : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_wrap_xml_content()
        {
            XNamespace ns = "demoNamespace";
            var xmlContent = new XDocument(new XElement(ns + "Document", new XElement(ns + "Value", "content")));

            var context = await Scenario.Define<Context>()
                .WithEndpoint<WrappingEndpoint>(e => e
                    .When(session => session.SendLocal(new MessageWithRawXml { Document = xmlContent })))
                .Done(c => c.MessageReceived)
                .Run();

            Assert.That(context.XmlPropertyValue.ToString(), Is.EqualTo(xmlContent.ToString()));
            Assert.That(context.XmlMessage.Root.Name.LocalName, Is.EqualTo(nameof(MessageWithRawXml)));
            Assert.That(context.XmlMessage.Root.Elements().Single().Name.LocalName, Is.EqualTo("Document"));
            Assert.That(context.XmlMessage.Root.Elements().Single().Elements().Single().Name.LocalName, Is.EqualTo("Document"));
            Assert.That(context.XmlMessage.Root.Elements().Single().Elements().Single().ToString(), Is.EqualTo(xmlContent.ToString()));
        }

        class Context : ScenarioContext
        {
            public bool MessageReceived { get; set; }

            public XDocument XmlMessage { get; set; }

            public XDocument XmlPropertyValue { get; set; }
        }

        class WrappingEndpoint : EndpointConfigurationBuilder
        {
            public WrappingEndpoint()
            {
                EndpointSetup<DefaultServer,Context>((config,context) =>
                {
                    config.UseSerialization<XmlSerializer>(); // wrapping is enabled by default
                    config.RegisterMessageMutator(new IncomingMutator(context));
                });
            }

            class RawXmlMessageHandler : IHandleMessages<MessageWithRawXml>
            {
                public RawXmlMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(MessageWithRawXml messageWithRawXml, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    testContext.MessageReceived = true;
                    testContext.XmlPropertyValue = messageWithRawXml.Document;

                    return Task.FromResult(0);
                }

                Context testContext;
            }

            public class IncomingMutator : IMutateIncomingTransportMessages
            {
                public IncomingMutator(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task MutateIncoming(MutateIncomingTransportMessageContext context)
                {
                    testContext.XmlMessage = XDocument.Parse(Encoding.UTF8.GetString(context.Body));

                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class MessageWithRawXml : ICommand
        {
            public XDocument Document { get; set; }
        }
    }
}