namespace NServiceBus.AcceptanceTests.Serialization;

using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using MessageMutator;
using NUnit.Framework;

public class When_no_content_type : NServiceBusAcceptanceTest
{
    [Test]
    public Task Should_handle_message() =>
        Scenario.Define<Context>()
            .WithEndpoint<EndpointViaType>(b => b.When(
                (session, _) => session.SendLocal(new Message
                {
                    Property = "value"
                })))
            .Run();

    public class Context : ScenarioContext
    {
        public bool ReceivedMessage { get; set; }
    }

    public class EndpointViaType : EndpointConfigurationBuilder
    {
        public EndpointViaType() => EndpointSetup<DefaultServer>(config => config.RegisterMessageMutator(new ContentTypeMutator()));

        [Handler]
        public class Handler(Context testContext) : IHandleMessages<Message>
        {
            public Task Handle(Message request, IMessageHandlerContext context)
            {
                testContext.ReceivedMessage = request.Property == "value";
                testContext.MarkAsCompleted(testContext.ReceivedMessage);
                return Task.CompletedTask;
            }
        }

        class ContentTypeMutator : IMutateIncomingTransportMessages
        {
            public Task MutateIncoming(MutateIncomingTransportMessageContext context)
            {
                context.Headers.Remove(Headers.ContentType);
                return Task.CompletedTask;
            }
        }
    }

    public class Message : IMessage
    {
        public string Property { get; set; }
    }
}