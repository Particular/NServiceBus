namespace NServiceBus.AcceptanceTests.Recoverability.Retries
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;
    using Transport;

    public class When_performing_slr_with_non_min_policy : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_execute_twice()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<RetryEndpoint>(b => b
                    .When(bus => bus.SendLocal(new MessageToBeRetried()))
                    .DoNotFailOnErrorMessages())
                .Done(c => c.MessageSentToErrorQueue)
                .Run();

            Assert.AreEqual(context.Count, 2);
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
                        .CustomPolicy(RetryPolicy);
                    configure.Notifications.Errors.MessageSentToErrorQueue += (sender, message) => { scenarioContext.MessageSentToErrorQueue = true; };
                });
            }

            RecoverabilityAction RetryPolicy(RecoverabilityConfig config, ErrorContext context)
            {
                if (count == 0)
                {
                    count++;
                    return RecoverabilityAction.DelayedRetry(TimeSpan.FromMilliseconds(10));
                }
                return RecoverabilityAction.MoveToError();
            }

            int count;

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

        [Serializable]
        public class MessageToBeRetried : IMessage
        {
        }
    }
}