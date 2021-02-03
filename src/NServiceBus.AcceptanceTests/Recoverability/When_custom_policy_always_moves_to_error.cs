namespace NServiceBus.AcceptanceTests.Recoverability
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    public class When_custom_policy_always_moves_to_error : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_execute_only_once_and_send_to_error_queue()
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

            Assert.AreEqual(1, context.Count);
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
                    configure.EnableFeature<TimeoutManager>();
                    configure.Recoverability()
                        .CustomPolicy((cfg, errorContext) => RecoverabilityAction.MoveToError(cfg.Failed.ErrorQueue));
                });
            }

            class MessageToBeRetriedHandler : IHandleMessages<MessageToBeRetried>
            {
                public MessageToBeRetriedHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(MessageToBeRetried message, IMessageHandlerContext context)
                {
                    testContext.Count++;
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