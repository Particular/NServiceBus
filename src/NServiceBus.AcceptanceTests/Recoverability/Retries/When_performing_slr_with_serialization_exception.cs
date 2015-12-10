﻿namespace NServiceBus.AcceptanceTests.Recoverability.Retries
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NServiceBus.Features;
    using NServiceBus.MessageMutator;
    using NUnit.Framework;

    public class When_performing_slr_with_serialization_exception : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_preserve_the_original_body_for_serialization_exceptions()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<RetryEndpoint>(b =>
                {
                    b.When(bus => bus.SendLocal(new MessageToBeRetried()));
                    b.DoNotFailOnErrorMessages();
                })
                .Done(c => c.ChecksumOfErrorQueueMessage != default(byte))
                .Run();

            Assert.AreEqual(context.OriginalBodyChecksum, context.ChecksumOfErrorQueueMessage, "The body of the message sent to slr should be the same as the original message coming off the queue");
        }
        public class Context : ScenarioContext
        {
            public byte ChecksumOfErrorQueueMessage { get; set; }
            public byte OriginalBodyChecksum { get; set; }
        }

        public class RetryEndpoint : EndpointConfigurationBuilder
        {
            public RetryEndpoint()
            {
                EndpointSetup<DefaultServer>(configure =>
                {
                    configure.DisableFeature<FirstLevelRetries>();
                    configure.EnableFeature<SecondLevelRetries>();
                    configure.EnableFeature<TimeoutManager>();
                    configure.RegisterComponents(c => c.ConfigureComponent<BodyMutator>(DependencyLifecycle.InstancePerCall));
                    var context = (Context) ScenarioContext;
                    configure.Faults().AddFaultNotification(message =>
                    {
                        context.ChecksumOfErrorQueueMessage = Checksum(message.Body);
                    });
                })
                    .WithConfig<SecondLevelRetriesConfig>(c => c.TimeIncrease = TimeSpan.FromSeconds(1));
            }

            public static byte Checksum(byte[] data)
            {
                var longSum = data.Sum(x => (long)x);
                return unchecked((byte)longSum);
            }

            class BodyMutator : IMutateOutgoingTransportMessages, IMutateIncomingTransportMessages
            {
                public Context Context { get; set; }

                public Task MutateIncoming(MutateIncomingTransportMessageContext transportMessage)
                {
                    var originalBody = transportMessage.Body;

                    Context.OriginalBodyChecksum = Checksum(originalBody);

                    var decryptedBody = new byte[originalBody.Length];

                    Buffer.BlockCopy(originalBody, 0, decryptedBody, 0, originalBody.Length);

                    //decrypt
                    decryptedBody[0]++;
                    decryptedBody[1]++;

                    transportMessage.Body = decryptedBody;
                    return Task.FromResult(0);
                }

                public Task MutateOutgoing(MutateOutgoingTransportMessageContext context)
                {
                    context.OutgoingBody[0]--;
                    return Task.FromResult(0);
                }
            }

        }

        [Serializable]
        public class MessageToBeRetried : IMessage
        {
        }
    }
}