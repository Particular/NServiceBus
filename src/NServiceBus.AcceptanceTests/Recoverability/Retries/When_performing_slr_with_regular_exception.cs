namespace NServiceBus.AcceptanceTests.Recoverability.Retries
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Configuration.AdvanceExtensibility;
    using EndpointTemplates;
    using Features;
    using MessageMutator;
    using NServiceBus.Config;
    using NUnit.Framework;

    public class When_performing_slr_with_regular_exception : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_preserve_the_original_body_for_regular_exceptions()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<RetryEndpoint>(b => b
                    .When(session => session.SendLocal(new MessageToBeRetried()))
                    .DoNotFailOnErrorMessages())
                .Done(c => c.SlrChecksum != default(byte))
                .Run(TimeSpan.FromSeconds(120));

            Assert.AreEqual(context.OriginalBodyChecksum, context.SlrChecksum, "The body of the message sent to slr should be the same as the original message coming off the queue");
        }

        class Context : ScenarioContext
        {
            public byte OriginalBodyChecksum { get; set; }
            public byte SlrChecksum { get; set; }
        }

        public class RetryEndpoint : EndpointConfigurationBuilder
        {
            public RetryEndpoint()
            {
                EndpointSetup<DefaultServer>((configure, context) =>
                {
                    var scenarioContext = (Context) context.ScenarioContext;
                    configure.DisableFeature<FirstLevelRetries>();
                    configure.EnableFeature<SecondLevelRetries>();
                    configure.EnableFeature<TimeoutManager>();
                    configure.GetSettings().Get<Notifications>().Errors.MessageSentToErrorQueue += (sender, message) => { scenarioContext.SlrChecksum = Checksum(message.Body); };
                    configure.RegisterComponents(c => c.ConfigureComponent<BodyMutator>(DependencyLifecycle.InstancePerCall));
                })
                    .WithConfig<SecondLevelRetriesConfig>(c => c.TimeIncrease = TimeSpan.FromMilliseconds(1));
            }

            public static byte Checksum(byte[] data)
            {
                var longSum = data.Sum(x => (long) x);
                return unchecked((byte) longSum);
            }

            class BodyMutator : IMutateOutgoingTransportMessages, IMutateIncomingTransportMessages
            {
                public BodyMutator(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task MutateIncoming(MutateIncomingTransportMessageContext transportMessage)
                {
                    var originalBody = transportMessage.Body;

                    testContext.OriginalBodyChecksum = Checksum(originalBody);

                    var decryptedBody = new byte[originalBody.Length];

                    Buffer.BlockCopy(originalBody, 0, decryptedBody, 0, originalBody.Length);

                    //decrypt
                    decryptedBody[0]++;

                    transportMessage.Body = decryptedBody;
                    return Task.FromResult(0);
                }

                public Task MutateOutgoing(MutateOutgoingTransportMessageContext context)
                {
                    context.OutgoingBody[0]--;
                    return Task.FromResult(0);
                }

                Context testContext;
            }

            class MessageToBeRetriedHandler : IHandleMessages<MessageToBeRetried>
            {
                public Task Handle(MessageToBeRetried message, IMessageHandlerContext context)
                {
                    throw new SimulatedException();
                }
            }
        }

        [Serializable]
        public class MessageToBeRetried : IMessage
        {
        }
    }
}