namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
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
                EndpointSetup<DefaultServer>();
            }

            public class Handler : IHandleMessages<Message>
            {
                public Context Context { get; set; }

                public Task Handle(Message request, IMessageHandlerContext context)
                {
                    Context.ReceivedMessage = request.Property == "value";
                    return Task.FromResult(0);
                }
            }

            class ContentTypeMutator : IMutateIncomingTransportMessages, INeedInitialization
            {
                public Context Context { get; set; }

                public Task MutateIncoming(MutateIncomingTransportMessageContext context)
                {
                    context.Headers.Remove(Headers.ContentType);
                    return Task.FromResult(0);
                }

                public void Customize(EndpointConfiguration configuration)
                {
                    configuration.RegisterComponents(c =>
                        c.ConfigureComponent<ContentTypeMutator>(DependencyLifecycle.InstancePerCall));
                }
            }
        }

        [Serializable]
        public class Message : IMessage
        {
            public string Property { get; set; }
        }
    }
}