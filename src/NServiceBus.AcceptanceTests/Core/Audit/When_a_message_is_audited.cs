namespace NServiceBus.AcceptanceTests.Audit
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using MessageMutator;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NUnit.Framework;

    public class When_a_message_is_being_audited : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_allow_body_to_be_sent_separately()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithSeparateBodyStorage>(b => b.When((session, c) => session.SendLocal(new MessageToBeAudited())))
                .WithEndpoint<AuditSpyEndpoint>()
                .Done(c => c.AuditMessageReceived)
                .Run();

            Assert.True(context.BodyWasEmpty);
        }

        public class Context : ScenarioContext
        {
            public bool AuditMessageReceived { get; set; }
            public bool BodyWasEmpty { get; set; }
        }

        public class EndpointWithSeparateBodyStorage : EndpointConfigurationBuilder
        {
            public EndpointWithSeparateBodyStorage()
            {
                EndpointSetup<DefaultServer, Context>((config, context) =>
                 {
                     config.AuditProcessedMessagesTo<AuditSpyEndpoint>();
                     config.Pipeline.Register(typeof(AuditBodyStorageBehavior), "Simulate writing the body to a separate storage and pass a null body to the transport");
                 });
            }

            public class AuditBodyStorageBehavior : Behavior<IDispatchContext>
            {
                public override Task Invoke(IDispatchContext context, Func<Task> next)
                {
                    foreach (var operation in context.Operations)
                    {
                        var unicastAddress = operation.AddressTag as UnicastAddressTag;

                        if (unicastAddress?.Destination != Conventions.EndpointNamingConvention(typeof(AuditSpyEndpoint)))
                        {
                            continue;
                        }

                        operation.Message.UpdateBody(null); //TODO: Would ReadOnlyMemory<byte>.Empty be better?
                    }
                    return next();
                }
            }

            public class MessageToBeAuditedHandler : IHandleMessages<MessageToBeAudited>
            {
                public Task Handle(MessageToBeAudited message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }
            }
        }

        class AuditSpyEndpoint : EndpointConfigurationBuilder
        {
            public AuditSpyEndpoint()
            {
                EndpointSetup<DefaultServer, Context>((config, context) => config.RegisterMessageMutator(new BodySpy(context)));
            }

            class BodySpy : IMutateIncomingTransportMessages
            {
                public BodySpy(Context context)
                {
                    this.context = context;
                }

                public Task MutateIncoming(MutateIncomingTransportMessageContext transportMessage)
                {
                    context.BodyWasEmpty = transportMessage.Body.Length == 0;
                    context.AuditMessageReceived = true;
                    return Task.FromResult(0);
                }

                Context context;
            }
        }

        public class MessageToBeAudited : IMessage
        {
        }
    }
}