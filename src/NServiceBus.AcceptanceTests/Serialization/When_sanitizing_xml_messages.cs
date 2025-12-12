namespace NServiceBus.AcceptanceTests.Serialization;

using System.Text;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using MessageMutator;
using NUnit.Framework;

public class When_sanitizing_xml_messages : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_remove_illegal_characters_from_messages()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointSanitizingInput>(e => e
                .When(session => session.SendLocal(new SimpleMessage
                {
                    Value = "Hello World!"
                })))
            .Run();

        Assert.That(context.Input, Is.EqualTo("Hello World!"));
    }

    class Context : ScenarioContext
    {
        public string Input { get; set; }
    }

    class EndpointSanitizingInput : EndpointConfigurationBuilder
    {
        public EndpointSanitizingInput() =>
            EndpointSetup<DefaultServer>(c =>
            {
                c.UseSerialization<XmlSerializer>().SanitizeInput();
                c.RegisterMessageMutator(new InjectInvalidCharMutator());
            });

        class SimpleMessageHandler(Context scenarioContext) : IHandleMessages<SimpleMessage>
        {
            public Task Handle(SimpleMessage message, IMessageHandlerContext context)
            {
                scenarioContext.Input = message.Value;
                scenarioContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }

        class InjectInvalidCharMutator : IMutateIncomingTransportMessages
        {
            public Task MutateIncoming(MutateIncomingTransportMessageContext context)
            {
                var body = Encoding.UTF8.GetString(context.Body.ToArray());
                var invalidChar = char.ConvertFromUtf32(0x8);

                context.Body = Encoding.UTF8.GetBytes(body.Replace("Hello World!", $"{invalidChar}Hello {invalidChar}World!{invalidChar}"));

                return Task.CompletedTask;
            }
        }
    }

    public class SimpleMessage : ICommand
    {
        public string Value { get; set; }
    }
}