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
                .Done(c => Context.ThrottleModeEntered && Context.failuresBeforeThrottling >= NumberOfConsecutiveFailuresBeforeThrottling)
                .Run();
        }

        [Test]
        public async Task Should_wait_the_configured_delay_between_processing_attempts_in_throttled_mode()
        {
            NumberOfConsecutiveFailuresBeforeThrottling = 1;
            TimeToWaitBetweenThrottledAttempts = TimeSpan.FromSeconds(2);

            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithFailingHandler>(b => b
                    .DoNotFailOnErrorMessages()
                    .When(async (session, ctx) =>
                    {
                        for (var x = 0; x < 5; x++)
                        {
                            await session.SendLocal(new InitiatingMessage
                            {
                                Id = ctx.TestRunId
                            });
                        }
                    })
                )
                .Done(c => Context.ThrottleModeEntered && Context.TimeBetweenProcessingAttempts >= TimeToWaitBetweenThrottledAttempts)
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
                .Done(c => Context.failuresBeforeThrottling == 10 && !Context.ThrottleModeEntered)
                .Run();
        }

        class Context : ScenarioContext
        {
            public static bool ThrottleModeEntered { get; set; }
            public static int failuresBeforeThrottling;
            public static DateTime LastProcessedTimeStamp { get; set; }
            public static TimeSpan TimeBetweenProcessingAttempts { get; set; }
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

                    recoverability.SystemOutageRateLimiting(d =>
                    {
                        d.NumberOfConsecutiveFailuresBeforeThrottling(NumberOfConsecutiveFailuresBeforeThrottling);
                        d.TimeToWaitBeforeThrottledProcessingAttempts(TimeToWaitBetweenThrottledAttempts);

                        d.OnThrottledModeStarted(() =>
                        {
                            Context.ThrottleModeEntered = true;

                            return Task.FromResult(0);
                        });
                    });
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