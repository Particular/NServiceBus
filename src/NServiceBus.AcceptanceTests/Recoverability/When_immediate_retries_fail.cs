namespace NServiceBus.AcceptanceTests.Recoverability
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    public class When_immediate_retries_fail : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_do_delayed_retries()
        {
            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                .WithEndpoint<DelayedRetryEndpoint>(b => b
                    .When((session, ctx) => session.SendLocal(new MessageToBeRetried
                    {
                        Id = ctx.Id
                    }))
                    .DoNotFailOnErrorMessages())
                .Done(c => c.NumberOfRetriesAttempted >= 1)
                .Run();

            Assert.GreaterOrEqual(1, context.NumberOfRetriesAttempted, "Should retry one or more times");
        }

        static TimeSpan Delay = TimeSpan.FromMilliseconds(1);

        class Context : ScenarioContext
        {
            public Guid Id { get; set; }

            public int NumberOfTimesInvoked { get; set; }

            public int NumberOfRetriesAttempted => NumberOfTimesInvoked - 1 < 0 ? 0 : NumberOfTimesInvoked - 1;
        }

        public class DelayedRetryEndpoint : EndpointConfigurationBuilder
        {
            public DelayedRetryEndpoint()
            {
                EndpointSetup<DefaultServer>(config =>
                {
                    config.EnableFeature<TimeoutManager>();
                    config.Recoverability().Immediate(i => i.NumberOfRetries(0));
                    config.Recoverability()
                        .Delayed(settings =>
                        {
                            settings.NumberOfRetries(1);
                            settings.TimeIncrease(Delay);
                        });
                });
            }

            class MessageToBeRetriedHandler : IHandleMessages<MessageToBeRetried>
            {
                public Context TestContext { get; set; }

                public Task Handle(MessageToBeRetried message, IMessageHandlerContext context)
                {
                    if (message.Id != TestContext.Id)
                    {
                        return Task.FromResult(0); // messages from previous test runs must be ignored
                    }

                    TestContext.NumberOfTimesInvoked++;

                    throw new SimulatedException();
                }
            }
        }

        public class MessageToBeRetried : IMessage
        {
            public Guid Id { get; set; }
        }
    }
}