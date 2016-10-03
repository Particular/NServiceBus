namespace NServiceBus.AcceptanceTests.Recoverability.Retries
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    public class When_custom_policy_always_moves_to_error : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_execute_once()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<RetryEndpoint>(b => b
                    .When(bus => bus.SendLocal(new MessageToBeRetried()))
                    .DoNotFailOnErrorMessages())
                .Done(c => c.MessageSentToErrorQueue)
                .Run();

            Assert.AreEqual(context.Count, 1);
        }

        class Context : ScenarioContext
        {
            public bool MessageSentToErrorQueue { get; set; }
            public int Count { get; set; }
        }

        public class RetryEndpoint : EndpointConfigurationBuilder
        {
            public RetryEndpoint()
            {
                EndpointSetup<DefaultServer>((configure, context) =>
                {
                    var scenarioContext = (Context) context.ScenarioContext;
                    configure.EnableFeature<TimeoutManager>();
                    configure.Recoverability()
                        .CustomPolicy((cfg, errorContext) => RecoverabilityAction.MoveToError(cfg.Failed.ErrorQueue));
                    configure.Notifications.Errors.MessageSentToErrorQueue += (sender, message) => { scenarioContext.MessageSentToErrorQueue = true; };
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