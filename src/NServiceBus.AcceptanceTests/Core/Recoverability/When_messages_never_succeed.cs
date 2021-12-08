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
                .Done(c => Context.ThrottleModeEntered && Context.FailuresBeforeThrottling >= NumberOfConsecutiveFailuresBeforeThrottling)
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
                .Done(c => Context.FailuresBeforeThrottling == 10 && !Context.ThrottleModeEntered)
                .Run();
        }

        class Context : ScenarioContext
        {
            public static bool ThrottleModeEntered { get; set; }
            public static int failuresBeforeThrottling;
            public static DateTime LastProcessedTimeStamp { get; set; }
            public static TimeSpan TimeBetweenProcessingAttempts { get; set; }

            public static int FailuresBeforeThrottling => failuresBeforeThrottling;
        }

        class EndpointWithFailingHandler : EndpointConfigurationBuilder
        {
            public EndpointWithFailingHandler()
            {
                EndpointSetup<DefaultServer>((config, context) =>
                {
                    config.LimitMessageProcessingConcurrencyTo(3);

                    var recoverability = config.Recoverability();

                    recoverability.Immediate(i => i.NumberOfRetries(0));
                    recoverability.Delayed(d => d.NumberOfRetries(0));

                    var rateLimitingSettings = new RateLimitSettings(TimeToWaitBetweenThrottledAttempts, () =>
                        {
                            Context.ThrottleModeEntered = true;

                            return Task.FromResult(0);
                        });

                    recoverability.OnConsecutiveFailures(NumberOfConsecutiveFailuresBeforeThrottling, rateLimitingSettings);
                });
            }

            class InitiatingHandler : IHandleMessages<InitiatingMessage>
            {
                public Task Handle(InitiatingMessage initiatingMessage, IMessageHandlerContext context)
                {
                    if (Context.ThrottleModeEntered)
                    {
                        Context.TimeBetweenProcessingAttempts = DateTime.Now - Context.LastProcessedTimeStamp;
                    }

                    Context.LastProcessedTimeStamp = DateTime.Now;

                    Interlocked.Increment(ref Context.failuresBeforeThrottling);

                    throw new SimulatedException("THIS IS A MESSAGE THAT WILL NEVER SUCCEED");
                }
            }
        }

        public class InitiatingMessage : IMessage
        {
            public Guid Id { get; set; }
        }
    }
}