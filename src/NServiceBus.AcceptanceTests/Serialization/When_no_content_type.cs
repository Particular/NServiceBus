namespace NServiceBus.AcceptanceTests.Serialization
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using MessageMutator;
    using NUnit.Framework;

    public class When_no_content_type : NServiceBusAcceptanceTest
    {
        [Test]
        public Task Should_handle_message()
        {
            return Scenario.Define<Context>()
                .WithEndpoint<EndpointViaType>(b => b.When(
                    (session, c) => session.SendLocal(new Message
                    {
                        Property = "value"
                    })))
                .Done(c => c.ReceivedMessage)
                .Run();
        }

        public class Context : ScenarioContext
        {
            public bool ReceivedMessage { get; set; }
        }

        public class EndpointViaType : EndpointConfigurationBuilder
        {
            public EndpointViaType()
            {
                EndpointSetup<DefaultServer>(config => config.RegisterMessageMutator(new ContentTypeMutator()));
            }

            public class Handler : IHandleMessages<Message>
            {
                public Handler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(Message request, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    testContext.ReceivedMessage = request.Property == "value";
                    return Task.FromResult(0);
                }

                Context testContext;
            }

            class ContentTypeMutator : IMutateIncomingTransportMessages
            {
                public Task MutateIncoming(MutateIncomingTransportMessageContext context)
                {
                    context.Headers.Remove(Headers.ContentType);
                    return Task.FromResult(0);
                }
            }
        }


        public class Message : IMessage
        {
            public string Property { get; set; }
        }
    }
}