namespace NServiceBus.AcceptanceTests.Core.Recoverability;

using System;
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
                .When(async (session, _) =>
                {
                    for (var x = 0; x < 3; x++)
                    {
                        await session.SendLocal(new InitiatingMessage());
                    }
                })
            )
            .Run();
    }

    public class Context : ScenarioContext
    {
        public bool ThrottleModeEntered { get; set; }
        public DateTime LastProcessedTimeStamp { get; set; }
        public TimeSpan TimeBetweenProcessingAttempts { get; set; }

        public void MaybeCompleted() => MarkAsCompleted(ThrottleModeEntered, TimeBetweenProcessingAttempts >= TimeSpan.FromSeconds(2));
    }

    public class EndpointWithFailingHandler : EndpointConfigurationBuilder
    {
        public EndpointWithFailingHandler() =>
            EndpointSetup<DefaultServer>((config, context) =>
            {
                config.LimitMessageProcessingConcurrencyTo(1);

                var scenarioContext = (Context)context.ScenarioContext;
                var recoverability = config.Recoverability();

                recoverability.Immediate(i => i.NumberOfRetries(0));
                recoverability.Delayed(d => d.NumberOfRetries(0));

                var rateLimitingSettings = new RateLimitSettings(TimeSpan.FromSeconds(2), cancellation =>
                {
                    scenarioContext.ThrottleModeEntered = true;
                    scenarioContext.MaybeCompleted();

                    return Task.CompletedTask;
                });

                recoverability.OnConsecutiveFailures(1, rateLimitingSettings);
            });

        [Handler]
        public class InitiatingHandler(Context testContext) : IHandleMessages<InitiatingMessage>
        {
            public Task Handle(InitiatingMessage initiatingMessage, IMessageHandlerContext context)
            {
                if (testContext.ThrottleModeEntered)
                {
                    testContext.TimeBetweenProcessingAttempts = DateTime.UtcNow - testContext.LastProcessedTimeStamp;
                }

                testContext.LastProcessedTimeStamp = DateTime.UtcNow;

                testContext.MaybeCompleted();
                throw new SimulatedException("THIS IS A MESSAGE THAT WILL NEVER SUCCEED");
            }
        }
    }

    public class InitiatingMessage : IMessage;
}