namespace NServiceBus.AcceptanceTests.Recoverability.Retries
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NServiceBus.Config;
    using NUnit.Framework;

    public class When_performing_slr_with_flr_disabled : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_reschedule_message_three_times_by_default()
        {
            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                .WithEndpoint<RetryEndpoint>(b => b
                    .When((session, ctx) => session.SendLocal(new MessageToBeRetried {Id = ctx.Id}))
                    .DoNotFailOnErrorMessages())
                .Done(c => c.ReceiveCount >= 4)
                .Run();

            Assert.AreEqual(4, context.ReceiveCount, "Message should be delivered 4 times. Once initially and retried 3 times by SLR");
        }

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
                    configure.EnableFeature<TimeoutManager>();
                }).WithConfig<SecondLevelRetriesConfig>(c => c.TimeIncrease = TimeSpan.FromMilliseconds(1));
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

        [Serializable]
        public class MessageToBeRetried : IMessage
        {
            public Guid Id { get; set; }
        }

    }
}