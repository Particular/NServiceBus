namespace NServiceBus.AcceptanceTests.Core.Recoverability;

using System;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Support;
using EndpointTemplates;
using MessageMutator;
using NUnit.Framework;

public class When_failing_mutated_message : NServiceBusAcceptanceTest
{
    [Test]
    public void Should_preserve_the_original_body()
    {
        Requires.DelayedDelivery();

        var exception = Assert.ThrowsAsync<MessageFailedException>(async () => await Scenario.Define<Context>()
            .WithEndpoint<RetryEndpoint>(b => b
                .When(session => session.SendLocal(new MessageToBeRetried())))
            .Run());

        var context = (Context)exception.ScenarioContext;
        var errorBody = context.FailedMessages.Single().Value.Single().Body;

        Assert.That(errorBody.ToArray(), Is.EqualTo(context.OriginalBody).AsCollection, "The body of the message sent to delayed retry should be the same as the original message coming off the queue");
    }

    public class Context : ScenarioContext
    {
        public byte[] OriginalBody { get; set; }
    }

    public class RetryEndpoint : EndpointConfigurationBuilder
    {
        public RetryEndpoint() =>
            EndpointSetup<DefaultServer, Context>((config, context) =>
            {
                config.RegisterMessageMutator(new BodyMutator(context));
                var recoverability = config.Recoverability();
                recoverability.Delayed(settings => settings.NumberOfRetries(1).TimeIncrease(TimeSpan.FromMilliseconds(1)));
                recoverability.Immediate(settings => settings.NumberOfRetries(3));
            });

        class BodyMutator(Context testContext) : IMutateOutgoingTransportMessages, IMutateIncomingTransportMessages
        {
            public Task MutateIncoming(MutateIncomingTransportMessageContext transportMessage)
            {
                var originalBody = transportMessage.Body;
                testContext.OriginalBody = originalBody.ToArray();
                var newBody = new byte[originalBody.Length];
                Buffer.BlockCopy(originalBody.ToArray(), 0, newBody, 0, originalBody.Length);
                //corrupt
                newBody[1]++;
                transportMessage.Body = newBody;
                return Task.CompletedTask;
            }

            public Task MutateOutgoing(MutateOutgoingTransportMessageContext context) => Task.CompletedTask;
        }

        [Handler]
        public class MessageToBeRetriedHandler : IHandleMessages<MessageToBeRetried>
        {
            public Task Handle(MessageToBeRetried message, IMessageHandlerContext context) => Task.CompletedTask;
        }
    }

    public class MessageToBeRetried : IMessage;
}