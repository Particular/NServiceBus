namespace NServiceBus.AcceptanceTests.Serialization
{
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
                .Done(c => c.MessageReceived)
                .Run();

            Assert.AreEqual($"{CustomXmlNamespace}/{typeof(SimpleMessage).Namespace}", context.MessageNamespace);
        }

        class Context : ScenarioContext
        {
            public bool MessageReceived { get; set; }
            public string MessageNamespace { get; set; }
        }

        class EndpointUsingCustomNamespace : EndpointConfigurationBuilder
        {
            public EndpointUsingCustomNamespace()
            {
                EndpointSetup<DefaultServer, Context>((config, context) =>
                 {
                     config.UseSerialization<XmlSerializer>().Namespace(CustomXmlNamespace);
                     config.RegisterMessageMutator(new IncomingMutator(context));
                 });
            }

            class SimpleMessageHandler : IHandleMessages<SimpleMessage>
            {
                public SimpleMessageHandler(Context scenarioContext)
                {
                    this.scenarioContext = scenarioContext;
                }

                public Task Handle(SimpleMessage message, IMessageHandlerContext context)
                {
                    scenarioContext.MessageReceived = true;
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
                    var document = XDocument.Parse(Encoding.UTF8.GetString(context.Body.CreateCopy()));
                    var defaultNamespace = document.Root?.GetDefaultNamespace();
                    scenarioContext.MessageNamespace = defaultNamespace?.NamespaceName;
                    return Task.FromResult(0);
                }

                Context scenarioContext;
            }
        }

        public class SimpleMessage : ICommand
        {
        }
    }
}