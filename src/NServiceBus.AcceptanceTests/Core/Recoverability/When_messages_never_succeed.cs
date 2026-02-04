namespace NServiceBus.AcceptanceTests.Core.Recoverability;

using System;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_messages_never_succeed : NServiceBusAcceptanceTest
{
    static int NumberOfConsecutiveFailuresBeforeThrottling = 1;
    static readonly TimeSpan TimeToWaitBetweenThrottledAttempts = TimeSpan.FromSeconds(0);

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
                        await session.SendLocal(new InitiatingMessage());
                    }
                })
            )
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
                        await session.SendLocal(new InitiatingMessage());
                    }
                })
            )
            .Done(c => c is { FailuresBeforeThrottling: 10, ThrottleModeEntered: false })
            .Run();
    }

    public class Context : ScenarioContext
    {
        public bool ThrottleModeEntered { get; set; }
        public DateTime LastProcessedTimeStamp { get; set; }
        public TimeSpan TimeBetweenProcessingAttempts { get; set; }

        public int FailuresBeforeThrottling => failuresBeforeThrottling;

        public void MaybeCompleted() => MarkAsCompleted(ThrottleModeEntered, FailuresBeforeThrottling >= NumberOfConsecutiveFailuresBeforeThrottling);

        public void IncrementFailuresBeforeThrottling() => Interlocked.Increment(ref failuresBeforeThrottling);

        int failuresBeforeThrottling;
    }

    public class EndpointWithFailingHandler : EndpointConfigurationBuilder
    {
        public EndpointWithFailingHandler() =>
            EndpointSetup<DefaultServer>((config, context) =>
            {
                config.LimitMessageProcessingConcurrencyTo(3);
                var scenarioContext = (Context)context.ScenarioContext;

                var recoverability = config.Recoverability();

                recoverability.Immediate(i => i.NumberOfRetries(0));
                recoverability.Delayed(d => d.NumberOfRetries(0));

                var rateLimitingSettings = new RateLimitSettings(TimeToWaitBetweenThrottledAttempts, cancellation =>
                {
                    scenarioContext.ThrottleModeEntered = true;
                    scenarioContext.MaybeCompleted();

                    return Task.CompletedTask;
                });

                recoverability.OnConsecutiveFailures(NumberOfConsecutiveFailuresBeforeThrottling, rateLimitingSettings);
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

                testContext.IncrementFailuresBeforeThrottling();

                testContext.MaybeCompleted();
                throw new SimulatedException("THIS IS A MESSAGE THAT WILL NEVER SUCCEED");
            }
        }
    }

    public class InitiatingMessage : IMessage;
}