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
                .Done(c => c.ThrottleModeEntered && c.TimeBetweenProcessingAttempts >= TimeSpan.FromSeconds(2))
                .Run();
        }

        class Context : ScenarioContext
        {
            public bool ThrottleModeEntered { get; set; }
            public static int failuresBeforeThrottling;
            public DateTime LastProcessedTimeStamp { get; set; }
            public TimeSpan TimeBetweenProcessingAttempts { get; set; }
        }

        class EndpointWithFailingHandler : EndpointConfigurationBuilder
        {
            public EndpointWithFailingHandler()
            {
                EndpointSetup<DefaultServer>((config, context) =>
                {
                    var scenarioContext = (Context)context.ScenarioContext;
                    config.LimitMessageProcessingConcurrencyTo(1);

                    var recoverability = config.Recoverability();

                    recoverability.Immediate(i => i.NumberOfRetries(0));
                    recoverability.Delayed(d => d.NumberOfRetries(0));

                    var rateLimitingSettings = new RateLimitSettings(TimeSpan.FromSeconds(2), () =>
                    {
                        scenarioContext.ThrottleModeEntered = true;

                        return Task.FromResult(0);
                    });

                    recoverability.OnConsecutiveFailures(1, rateLimitingSettings);
                });
            }

            class InitiatingHandler : IHandleMessages<InitiatingMessage>
            {
                public InitiatingHandler(Context context)
                {
                    testContext = context;
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