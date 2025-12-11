namespace NServiceBus.AcceptanceTests.Audit;

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
            .Run();

        Assert.That(context.AuditChecksum, Is.EqualTo(context.OriginalBodyChecksum), "The body of the message sent to audit should be the same as the original message coming off the queue");
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
        public EndpointWithAuditOn() =>
            EndpointSetup<DefaultServer, Context>((config, context) =>
            {
                config.RegisterMessageMutator(new BodyMutator(context));
                config.AuditProcessedMessagesTo<AuditSpyEndpoint>();
            });

        class BodyMutator(Context testContext) : IMutateIncomingTransportMessages
        {
            public Task MutateIncoming(MutateIncomingTransportMessageContext context)
            {
                var originalBody = context.Body;

                testContext.OriginalBodyChecksum = Checksum(originalBody.ToArray());

                // modifying the body by adding a line break
                var modifiedBody = new byte[originalBody.Length + 1];

                Buffer.BlockCopy(originalBody.ToArray(), 0, modifiedBody, 0, originalBody.Length);

                modifiedBody[^1] = 13;

                context.Body = modifiedBody;
                return Task.CompletedTask;
            }
        }

        public class MessageToBeAuditedHandler : IHandleMessages<MessageToBeAudited>
        {
            public Task Handle(MessageToBeAudited message, IMessageHandlerContext context) => Task.CompletedTask;
        }
    }

    class AuditSpyEndpoint : EndpointConfigurationBuilder
    {
        public AuditSpyEndpoint() => EndpointSetup<DefaultServer, Context>((config, context) => config.RegisterMessageMutator(new BodySpy(context)));

        class BodySpy(Context context) : IMutateIncomingTransportMessages
        {
            public Task MutateIncoming(MutateIncomingTransportMessageContext transportMessage)
            {
                context.AuditChecksum = Checksum(transportMessage.Body.ToArray());
                return Task.CompletedTask;
            }
        }

        public class MessageToBeAuditedHandler(Context testContext) : IHandleMessages<MessageToBeAudited>
        {
            public Task Handle(MessageToBeAudited message, IMessageHandlerContext context)
            {
                if (message.RunId != testContext.RunId)
                {
                    return Task.CompletedTask;
                }

                testContext.MarkAsCompleted();

                return Task.CompletedTask;
            }
        }
    }


    public class MessageToBeAudited : IMessage
    {
        public Guid RunId { get; set; }
    }
}