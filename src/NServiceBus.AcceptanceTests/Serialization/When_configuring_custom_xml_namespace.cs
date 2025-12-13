namespace NServiceBus.AcceptanceTests.Serialization;

using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using AcceptanceTesting;
using EndpointTemplates;
using MessageMutator;
using NUnit.Framework;

public class When_configuring_custom_xml_namespace : NServiceBusAcceptanceTest
{
    const string CustomXmlNamespace = "https://particular.net";

    [Test]
    public async Task Should_use_as_root_namespace_in_messages()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointUsingCustomNamespace>(e => e
                .When(session => session.SendLocal(new SimpleMessage())))
            .Run();

        Assert.That(context.MessageNamespace, Is.EqualTo($"{CustomXmlNamespace}/{typeof(SimpleMessage).Namespace}"));
    }

    class Context : ScenarioContext
    {
        public string MessageNamespace { get; set; }
    }

    class EndpointUsingCustomNamespace : EndpointConfigurationBuilder
    {
        public EndpointUsingCustomNamespace() =>
            EndpointSetup<DefaultServer, Context>((config, context) =>
            {
                config.UseSerialization<XmlSerializer>().Namespace(CustomXmlNamespace);
                config.RegisterMessageMutator(new IncomingMutator(context));
            });

        class SimpleMessageHandler(Context scenarioContext) : IHandleMessages<SimpleMessage>
        {
            public Task Handle(SimpleMessage message, IMessageHandlerContext context)
            {
                scenarioContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }

        public class IncomingMutator(Context scenarioContext) : IMutateIncomingTransportMessages
        {
            public Task MutateIncoming(MutateIncomingTransportMessageContext context)
            {
                var document = XDocument.Parse(Encoding.UTF8.GetString(context.Body.ToArray()));
                var defaultNamespace = document.Root?.GetDefaultNamespace();
                scenarioContext.MessageNamespace = defaultNamespace?.NamespaceName;
                return Task.CompletedTask;
            }
        }
    }

    public class SimpleMessage : ICommand;
}