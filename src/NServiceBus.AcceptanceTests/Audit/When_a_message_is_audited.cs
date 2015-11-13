namespace NServiceBus.AcceptanceTests.Audit
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using EndpointTemplates;
    using AcceptanceTesting;
    using MessageMutator;
    using NUnit.Framework;

    public class When_a_message_is_audited : NServiceBusAcceptanceTest
    {

        [Test]
        public async Task Should_preserve_the_original_body()
        {
            var context = await Scenario.Define<Context>(c => { c.RunId = Guid.NewGuid(); })
                    .WithEndpoint<EndpointWithAuditOn>(b => b.When((bus, c) => bus.SendLocal(new MessageToBeAudited { RunId = c.RunId })))
                    .WithEndpoint<AuditSpyEndpoint>()
                    .Done(c => c.Done)
                    .Run();

            Assert.AreEqual(context.OriginalBodyChecksum, context.AuditChecksum, "The body of the message sent to audit should be the same as the original message coming off the queue");
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
                EndpointSetup<DefaultServer>()
                    .AuditTo<AuditSpyEndpoint>();
            }

            class BodyMutator : IMutateIncomingTransportMessages, INeedInitialization
            {
                public Context Context { get; set; }

                public Task MutateIncoming(MutateIncomingTransportMessageContext context)
                {
                    var originalBody = context.Body;

                    Context.OriginalBodyChecksum = Checksum(originalBody);

                    // modifying the body by adding a line break
                    var modifiedBody = new byte[originalBody.Length + 1];

                    Buffer.BlockCopy(originalBody, 0, modifiedBody, 0, originalBody.Length);

                    modifiedBody[modifiedBody.Length - 1] = 13;

                    context.Body = modifiedBody;
                    return Task.FromResult(0);
                }

                public void Customize(BusConfiguration configuration)
                {
                    configuration.RegisterComponents(c => c.ConfigureComponent<BodyMutator>(DependencyLifecycle.InstancePerCall));
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
                EndpointSetup<DefaultServer>();
            }

            class BodySpy : IMutateIncomingTransportMessages, INeedInitialization
            {
                public Context Context { get; set; }

                public Task MutateIncoming(MutateIncomingTransportMessageContext transportMessage)
                {
                    Context.AuditChecksum = Checksum(transportMessage.Body);
                    return Task.FromResult(0);
                }

                public void Customize(BusConfiguration configuration)
                {
                    configuration.RegisterComponents(c => c.ConfigureComponent<BodySpy>(DependencyLifecycle.InstancePerCall));
                }
            }

            public class MessageToBeAuditedHandler : IHandleMessages<MessageToBeAudited>
            {
                public Context TestContext { get; set; }

                public Task Handle(MessageToBeAudited message, IMessageHandlerContext context)
                {
                    if (message.RunId != TestContext.RunId)
                    {
                        return Task.FromResult(0);
                    }

                    TestContext.Done = true;

                    return Task.FromResult(0);
                }
            }
        }

        public static byte Checksum(byte[] data)
        {
            var longSum = data.Sum(x => (long)x);
            return unchecked((byte)longSum);
        }

        [Serializable]
        public class MessageToBeAudited : IMessage
        {
            public Guid RunId { get; set; }
        }
    }
}
