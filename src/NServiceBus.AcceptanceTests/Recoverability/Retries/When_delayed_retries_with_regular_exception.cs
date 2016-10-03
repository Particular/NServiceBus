namespace NServiceBus.AcceptanceTests.Recoverability.Retries
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using MessageMutator;
    using NUnit.Framework;

    public class When_delayed_retries_with_regular_exception : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_preserve_the_original_body_for_regular_exceptions()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<RetryEndpoint>(b => b
                    .When(session => session.SendLocal(new MessageToBeRetried()))
                    .DoNotFailOnErrorMessages())
                .Done(c => c.DelayedRetryChecksum != default(byte))
                .Run(TimeSpan.FromSeconds(120));

            Assert.AreEqual(context.OriginalBodyChecksum, context.DelayedRetryChecksum, "The body of the message sent to Delayed Retry should be the same as the original message coming off the queue");
        }

        class Context : ScenarioContext
        {
            public byte OriginalBodyChecksum { get; set; }
            public byte DelayedRetryChecksum { get; set; }
        }

        public class RetryEndpoint : EndpointConfigurationBuilder
        {
            public RetryEndpoint()
            {
                EndpointSetup<DefaultServer>((configure, context) =>
                {
                    var scenarioContext = (Context) context.ScenarioContext;
                    configure.EnableFeature<TimeoutManager>();
                    configure.Notifications.Errors.MessageSentToErrorQueue += (sender, message) => { scenarioContext.DelayedRetryChecksum = Checksum(message.Body); };
                    configure.RegisterComponents(c => c.ConfigureComponent<BodyMutator>(DependencyLifecycle.InstancePerCall));
                    var recoverability = configure.Recoverability();
                    recoverability.Delayed(settings => settings.TimeIncrease(TimeSpan.FromMilliseconds(1)));
                });
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

        
        public class MessageToBeRetried : IMessage
        {
        }
    }
}