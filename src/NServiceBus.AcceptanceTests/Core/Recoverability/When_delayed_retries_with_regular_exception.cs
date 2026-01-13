namespace NServiceBus.AcceptanceTests.Core.Recoverability;

using System;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Support;
using EndpointTemplates;
using MessageMutator;
using NUnit.Framework;

public class When_delayed_retries_with_regular_exception : NServiceBusAcceptanceTest
{
    [Test]
    public void Should_preserve_the_original_body_for_regular_exceptions()
    {
        Requires.DelayedDelivery();

        var exception = Assert.ThrowsAsync<MessageFailedException>(async () => await Scenario.Define<Context>()
            .WithEndpoint<RetryEndpoint>(b => b
                .When(session => session.SendLocal(new MessageToBeRetried())))
            .Run());

        var context = (Context)exception.ScenarioContext;
        var delayedRetryBody = context.FailedMessages.Single().Value.Single().Body;

        Assert.That(delayedRetryBody.ToArray(), Is.EqualTo(context.OriginalBody.ToArray()).AsCollection, "The body of the message sent to Delayed Retry should be the same as the original message coming off the queue");
    }

    class Context : ScenarioContext
    {
        public ReadOnlyMemory<byte> OriginalBody { get; set; }
    }

    public class RetryEndpoint : EndpointConfigurationBuilder
    {
        public RetryEndpoint() =>
            EndpointSetup<DefaultServer, Context>((config, context) =>
            {
                config.RegisterMessageMutator(new BodyMutator(context));
                var recoverability = config.Recoverability();
                recoverability.Delayed(settings => settings.NumberOfRetries(1).TimeIncrease(TimeSpan.FromMilliseconds(1)));
            });

        class BodyMutator(Context testContext) : IMutateOutgoingTransportMessages, IMutateIncomingTransportMessages
        {
            public Task MutateIncoming(MutateIncomingTransportMessageContext transportMessage)
            {
                var originalBody = transportMessage.Body;

                testContext.OriginalBody = originalBody;

                var decryptedBody = new byte[originalBody.Length];

                Buffer.BlockCopy(originalBody.ToArray(), 0, decryptedBody, 0, originalBody.Length);

                //decrypt
                decryptedBody[0]++;

                transportMessage.Body = decryptedBody;
                return Task.CompletedTask;
            }

            public Task MutateOutgoing(MutateOutgoingTransportMessageContext context)
            {
                var updatedBody = context.OutgoingBody.ToArray();
                updatedBody[0]--;

                context.OutgoingBody = new ReadOnlyMemory<byte>(updatedBody);
                return Task.CompletedTask;
            }
        }

        class MessageToBeRetriedHandler : IHandleMessages<MessageToBeRetried>
        {
            public Task Handle(MessageToBeRetried message, IMessageHandlerContext context) => throw new SimulatedException();
        }
    }

    public class MessageToBeRetried : IMessage;
}