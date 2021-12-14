namespace NServiceBus.AcceptanceTests.Core.Recoverability
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_messages_never_succeed : NServiceBusAcceptanceTest
    {
        public static int NumberOfConsecutiveFailuresBeforeThrottling = 1;
        public static TimeSpan TimeToWaitBetweenThrottledAttempts = TimeSpan.FromSeconds(0);

        [Test]
        public async Task Should_throttle_pipeline_after_configured_number_of_consecutive_failures()
        {
            NumberOfConsecutiveFailuresBeforeThrottling = 5;

            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithFailingHandler>(b => b
                    .DoNotFailOnErrorMessages()
                    .When(async (session, ctx) =>
                    {
                        for (var x = 0; x < 10; x++)
                        {
                            await session.SendLocal(new InitiatingMessage
                            {
                                Id = ctx.TestRunId
                            });
                        }
                    })
                )
                .Done(c => c.ThrottleModeEntered && c.FailuresBeforeThrottling >= NumberOfConsecutiveFailuresBeforeThrottling)
                .Run();
        }

        [Test]
        public async Task Should_not_throttle_pipeline_if_number_of_consecutive_failures_is_below_threshold()
        {
            NumberOfConsecutiveFailuresBeforeThrottling = 100;

            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithFailingHandler>(b => b
                    .DoNotFailOnErrorMessages()
                    .When(async (session, ctx) =>
                    {
                        for (var x = 0; x < 10; x++)
                        {
                            await session.SendLocal(new InitiatingMessage
                            {
                                Id = ctx.TestRunId
                            });
                        }
                    })
                )
                .Done(c => c.FailuresBeforeThrottling == 10 && !c.ThrottleModeEntered)
                .Run();
        }

        class Context : ScenarioContext
        {
            public bool ThrottleModeEntered { get; set; }
            public static int failuresBeforeThrottling;
            public DateTime LastProcessedTimeStamp { get; set; }
            public TimeSpan TimeBetweenProcessingAttempts { get; set; }

            public int FailuresBeforeThrottling => failuresBeforeThrottling;
        }

        class EndpointWithFailingHandler : EndpointConfigurationBuilder
        {
            public EndpointWithFailingHandler()
            {
                EndpointSetup<DefaultServer>((config, context) =>
                {
                    config.LimitMessageProcessingConcurrencyTo(3);
                    var scenarioContext = (Context)context.ScenarioContext;

                    var recoverability = config.Recoverability();

                    recoverability.Immediate(i => i.NumberOfRetries(0));
                    recoverability.Delayed(d => d.NumberOfRetries(0));

                    var rateLimitingSettings = new RateLimitSettings(TimeToWaitBetweenThrottledAttempts, () =>
                        {
                            scenarioContext.ThrottleModeEntered = true;

                            return Task.FromResult(0);
                        });

                    recoverability.OnConsecutiveFailures(NumberOfConsecutiveFailuresBeforeThrottling, rateLimitingSettings);
                });
            }

            class InitiatingHandler : IHandleMessages<InitiatingMessage>
            {
                public InitiatingHandler(Context testContext)
                {
                    this.testContext = testContext;
                }
                public Task Handle(InitiatingMessage initiatingMessage, IMessageHandlerContext context)
                {
                    if (testContext.ThrottleModeEntered)
                    {
                        testContext.TimeBetweenProcessingAttempts = DateTime.Now - testContext.LastProcessedTimeStamp;
                    }

                    testContext.LastProcessedTimeStamp = DateTime.Now;

                    Interlocked.Increment(ref Context.failuresBeforeThrottling);

                    throw new SimulatedException("THIS IS A MESSAGE THAT WILL NEVER SUCCEED");
                }
                Context testContext;
            }
        }

        public class InitiatingMessage : IMessage
        {
            public Guid Id { get; set; }
        }
    }
}