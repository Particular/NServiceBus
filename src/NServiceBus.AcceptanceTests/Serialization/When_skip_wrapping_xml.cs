namespace NServiceBus.AcceptanceTests.Serialization;

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
        XNamespace ns = "demoNamespace";
        var xmlContent = new XDocument(new XElement(ns + "Document", new XElement(ns + "Value", "content")));

        var context = await Scenario.Define<Context>()
            .WithEndpoint<NonWrappingEndpoint>(e => e
                .When(session => session.SendLocal(new MessageWithRawXml { Document = xmlContent })))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.XmlPropertyValue.ToString(), Is.EqualTo(xmlContent.ToString()));
            Assert.That(context.XmlMessage.Root.Name.LocalName, Is.EqualTo(nameof(MessageWithRawXml)));
            Assert.That(context.XmlMessage.Root.Elements().Single().ToString(), Is.EqualTo(xmlContent.ToString()));
        }
    }

    class Context : ScenarioContext
    {
        public XDocument XmlMessage { get; set; }
        public XDocument XmlPropertyValue { get; set; }
    }

    class NonWrappingEndpoint : EndpointConfigurationBuilder
    {
        public NonWrappingEndpoint() =>
            EndpointSetup<DefaultServer, Context>((config, context) =>
            {
                config.UseSerialization<XmlSerializer>().DontWrapRawXml();
                config.RegisterMessageMutator(new IncomingMutator(context));
            });

        class RawXmlMessageHandler(Context testContext) : IHandleMessages<MessageWithRawXml>
        {
            public Task Handle(MessageWithRawXml messageWithRawXml, IMessageHandlerContext context)
            {
                testContext.XmlPropertyValue = messageWithRawXml.Document;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }

        public class IncomingMutator(Context testContext) : IMutateIncomingTransportMessages
        {
            public Task MutateIncoming(MutateIncomingTransportMessageContext context)
            {
                testContext.XmlMessage = XDocument.Parse(Encoding.UTF8.GetString(context.Body.ToArray()));

                return Task.CompletedTask;
            }
        }
    }

    public class MessageWithRawXml : ICommand
    {
        public XDocument Document { get; set; }
    }
}