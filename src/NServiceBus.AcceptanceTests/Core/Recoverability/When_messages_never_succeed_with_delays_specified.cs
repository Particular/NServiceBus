namespace NServiceBus.AcceptanceTests.Core.Recoverability
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_messages_never_succeed_with_delays_specified : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_wait_the_configured_delay_between_processing_attempts_in_throttled_mode()
        {
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
                .Done(c => Context.ThrottleModeEntered && Context.TimeBetweenProcessingAttempts >= TimeSpan.FromSeconds(2))
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

                    var rateLimitingSettings = new RateLimitSettings(TimeSpan.FromSeconds(2), () =>
                    {
                        Context.ThrottleModeEntered = true;

                        return Task.FromResult(0);
                    });

                    recoverability.OnConsecutiveFailures(1, rateLimitingSettings);
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