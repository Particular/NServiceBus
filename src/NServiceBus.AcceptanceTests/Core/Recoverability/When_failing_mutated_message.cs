namespace NServiceBus.AcceptanceTests.Core.Recoverability
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using MessageMutator;
    using NUnit.Framework;

    public class When_failing_mutated_message : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_preserve_the_original_body()
        {
            Requires.DelayedDelivery();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<RetryEndpoint>(b => b
                    .When(session => session.SendLocal(new MessageToBeRetried()))
                    .DoNotFailOnErrorMessages())
                .Done(c => c.FailedMessages.Any())
                .Run();

            var errorBody = context.FailedMessages.Single().Value.Single().Body;

            CollectionAssert.AreEqual(context.OriginalBody, errorBody, "The body of the message sent to delayed retry should be the same as the original message coming off the queue");
        }

        class Context : ScenarioContext
        {
            public byte[] OriginalBody { get; set; }
        }

        public class RetryEndpoint : EndpointConfigurationBuilder
        {
            public RetryEndpoint()
            {
                EndpointSetup<DefaultServer, Context>((config, context) =>
                 {
                     config.RegisterMessageMutator(new BodyMutator(context));
                     config.Recoverability().Delayed(settings => settings.NumberOfRetries(1).TimeIncrease(TimeSpan.FromMilliseconds(1)));
                     config.Recoverability().Immediate(settings => settings.NumberOfRetries(3));
                 });
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
                    testContext.OriginalBody = originalBody;
                    var newBody = new byte[originalBody.Length];
                    Buffer.BlockCopy(originalBody, 0, newBody, 0, originalBody.Length);
                    //corrupt
                    newBody[1]++;
                    transportMessage.Body = newBody;
                    return Task.FromResult(0);
                }

                public Task MutateOutgoing(MutateOutgoingTransportMessageContext context)
                {
                    return Task.FromResult(0);
                }

                Context testContext;
            }

            class MessageToBeRetriedHandler : IHandleMessages<MessageToBeRetried>
            {
                public Task Handle(MessageToBeRetried message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }
            }
        }

        public class MessageToBeRetried : IMessage
        {
        }
    }
}