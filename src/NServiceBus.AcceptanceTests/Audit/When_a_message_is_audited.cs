namespace NServiceBus.AcceptanceTests.Audit
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using MessageMutator;
    using NUnit.Framework;

    public class When_a_message_is_audited : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_preserve_the_original_body()
        {
            var context = await Scenario.Define<Context>(c => { c.RunId = Guid.NewGuid(); })
                .WithEndpoint<EndpointWithAuditOn>(b => b.When((session, c) => session.SendLocal(new MessageToBeAudited
                {
                    RunId = c.RunId
                })))
                .WithEndpoint<AuditSpyEndpoint>()
                .Done(c => c.Done)
                .Run();

            Assert.AreEqual(context.OriginalBodyChecksum, context.AuditChecksum, "The body of the message sent to audit should be the same as the original message coming off the queue");
        }

        public static byte Checksum(byte[] data)
        {
            var longSum = data.Sum(x => (long)x);
            return unchecked((byte)longSum);
        }

        public class Context : ScenarioContext
        {
            public Guid RunId { get; set; }
            public bool Done { get; set; }
            public byte OriginalBodyChecksum { get; set; }
            public byte AuditChecksum { get; set; }
        }

        public class EndpointWithAuditOn : EndpointConfigurationBuilder
        {
            public EndpointWithAuditOn()
            {
                EndpointSetup<DefaultServer,Context>((config, context) =>
                {
                    config.RegisterMessageMutator(new BodyMutator(context));
                    config.AuditProcessedMessagesTo<AuditSpyEndpoint>();
                });
            }

            class BodyMutator : IMutateIncomingTransportMessages
            {
                public BodyMutator(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task MutateIncoming(MutateIncomingTransportMessageContext context)
                {
                    var originalBody = context.Body;

                    testContext.OriginalBodyChecksum = Checksum(originalBody);

                    // modifying the body by adding a line break
                    var modifiedBody = new byte[originalBody.Length + 1];

                    Buffer.BlockCopy(originalBody, 0, modifiedBody, 0, originalBody.Length);

                    modifiedBody[modifiedBody.Length - 1] = 13;

                    context.Body = modifiedBody;
                    return Task.FromResult(0);
                }

                Context testContext;
            }

            public class MessageToBeAuditedHandler : IHandleMessages<MessageToBeAudited>
            {
                public Task Handle(MessageToBeAudited message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    return Task.FromResult(0);
                }
            }
        }

        class AuditSpyEndpoint : EndpointConfigurationBuilder
        {
            public AuditSpyEndpoint()
            {
                EndpointSetup<DefaultServer,Context>((config, context) => config.RegisterMessageMutator(new BodySpy(context)));
            }

            class BodySpy : IMutateIncomingTransportMessages
            {
                public BodySpy(Context context)
                {
                    this.context = context;
                }

                public Task MutateIncoming(MutateIncomingTransportMessageContext transportMessage)
                {
                    context.AuditChecksum = Checksum(transportMessage.Body);
                    return Task.FromResult(0);
                }

                Context context;
            }

            public class MessageToBeAuditedHandler : IHandleMessages<MessageToBeAudited>
            {
                public MessageToBeAuditedHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(MessageToBeAudited message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    if (message.RunId != testContext.RunId)
                        return Task.FromResult(0);

                    testContext.Done = true;

                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }


        public class MessageToBeAudited : IMessage
        {
            public Guid RunId { get; set; }
        }
    }
}