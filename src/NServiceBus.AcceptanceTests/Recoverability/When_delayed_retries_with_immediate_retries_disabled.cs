namespace NServiceBus.AcceptanceTests.Recoverability
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_delayed_retries_with_immediate_retries_disabled : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_reschedule_message_the_configured_number_of_times()
        {
            Requires.NativeDeferralSupport();

            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                .WithEndpoint<RetryEndpoint>(b => b
                    .When((session, ctx) => session.SendLocal(new MessageToBeRetried {Id = ctx.Id}))
                    .DoNotFailOnErrorMessages())
                .Done(c => c.FailedMessages.Any())
                .Run();

            Assert.AreEqual(ConfiguredNumberOfDelayedRetries + 1, context.ReceiveCount, "Message should be delivered 4 times. Once initially and retried 3 times by Delayed Retries");
        }

        const int ConfiguredNumberOfDelayedRetries = 3;

        class Context : ScenarioContext
        {
            public Guid Id { get; set; }
            public int ReceiveCount { get; set; }
        }

        public class RetryEndpoint : EndpointConfigurationBuilder
        {
            public RetryEndpoint()
            {
                EndpointSetup<DefaultServer>((configure, context) =>
                {
                    var recoverability = configure.Recoverability();
                    recoverability.Delayed(settings => settings.TimeIncrease(TimeSpan.FromMilliseconds(1)).NumberOfRetries(ConfiguredNumberOfDelayedRetries));
                    recoverability.Immediate(settings => settings.NumberOfRetries(0));
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
                    if (testContext.Id == message.Id)
                    {
                        testContext.ReceiveCount++;

                        throw new SimulatedException();
                    }

                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class MessageToBeRetried : IMessage
        {
            public Guid Id { get; set; }
        }

    }
}