namespace NServiceBus.AcceptanceTests.Recoverability.Retries
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NServiceBus.Features;
    using NServiceBus.Transports;
    using NUnit.Framework;

    public class When_performing_slr_with_min_policy : NServiceBusAcceptanceTest
    {
        public class Context : ScenarioContext
        {
            public bool MessageSentToErrorQueue { get; set; }
            public int Count { get; set; }
        }

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


        public class RetryEndpoint : EndpointConfigurationBuilder
        {
            public RetryEndpoint()
            {
                EndpointSetup<DefaultServer>(configure =>
                {
                    configure.DisableFeature<FirstLevelRetries>();
                    configure.EnableFeature<SecondLevelRetries>();
                    configure.EnableFeature<TimeoutManager>();
                    configure.SecondLevelRetries().CustomRetryPolicy(RetryPolicy);
                })
                .WithConfig<SecondLevelRetriesConfig>(c => c.TimeIncrease = TimeSpan.FromMilliseconds(1));
            }

            static TimeSpan RetryPolicy(IncomingMessage transportMessage)
            {
                return TimeSpan.MinValue;
            }

            class ErrorNotificationSpy : IWantToRunWhenBusStartsAndStops
            {
                BusNotifications notifications;
                Context context;

                public ErrorNotificationSpy(Context context, BusNotifications notifications)
                {
                    this.notifications = notifications;
                    this.context = context;
                }

                public Task Start(IMessageSession session)
                {
                    notifications.Errors.MessageSentToErrorQueue += (sender, message) =>
                    {
                        context.MessageSentToErrorQueue = true;
                    };
                    return Task.FromResult(0);
                }

                public Task Stop(IMessageSession session)
                {
                    return Task.FromResult(0);
                }
            }

            class MessageToBeRetriedHandler : IHandleMessages<MessageToBeRetried>
            {
                Context testContext;

                public MessageToBeRetriedHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(MessageToBeRetried message, IMessageHandlerContext context)
                {
                    testContext.Count ++;
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