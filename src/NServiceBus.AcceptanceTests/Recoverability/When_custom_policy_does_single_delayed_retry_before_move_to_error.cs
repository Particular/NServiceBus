namespace NServiceBus.AcceptanceTests.Recoverability
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;
    using Transport;

    public class When_custom_policy_does_single_delayed_retry_before_move_to_error : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_execute_twice_and_send_to_error_queue()
        {
            var messageId = Guid.NewGuid().ToString();
            var context = await Scenario.Define<Context>()
                .WithEndpoint<RetryEndpoint>(b => b
                    .When(bus =>
                    {
                        var sendOptions = new SendOptions();
                        sendOptions.RouteToThisEndpoint();
                        sendOptions.SetMessageId(messageId);
                        return bus.Send(new MessageToBeRetried(), sendOptions);
                    })
                    .DoNotFailOnErrorMessages())
                .Done(c => c.FailedMessages.Any())
                .Run();

            Assert.AreEqual(context.Count, 2);
            Assert.AreEqual(messageId, context.FailedMessages.Single().Value.Single().MessageId);
        }

        class Context : ScenarioContext
        {
            public int Count { get; set; }
        }

        public class RetryEndpoint : EndpointConfigurationBuilder
        {
            public RetryEndpoint()
            {
                EndpointSetup<DefaultServer>((configure, context) =>
                {
                    configure.Recoverability()
                        .CustomPolicy(RetryPolicy);
                });
            }

            RecoverabilityAction RetryPolicy(RecoverabilityConfig config, ErrorContext context)
            {
                if (context.DelayedDeliveriesPerformed == 0)
                {
                    return RecoverabilityAction.DelayedRetry(TimeSpan.FromMilliseconds(10));
                }

                return RecoverabilityAction.MoveToError(config.Failed.ErrorQueue);
            }

            class MessageToBeRetriedHandler : IHandleMessages<MessageToBeRetried>
            {
                public MessageToBeRetriedHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(MessageToBeRetried message, IMessageHandlerContext context)
                {
                    testContext.Count ++;
                    throw new SimulatedException();
                }

                Context testContext;
            }
        }

        public class MessageToBeRetried : IMessage
        {
        }
    }
}