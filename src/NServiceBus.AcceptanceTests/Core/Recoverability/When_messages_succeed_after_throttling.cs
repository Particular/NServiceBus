namespace NServiceBus.AcceptanceTests.Core.Recoverability;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_messages_succeed_after_throttling : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_end_throttling_mode_after_a_single_successful_message()
    {
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
            .Run();
    }

    public class Context : ScenarioContext
    {
        public bool ThrottleModeEntered { get; set; }
        public bool ThrottleModeEnded { get; set; }
        public bool MessageProcessedNormally { get; set; }

        public void MaybeCompleted() => MarkAsCompleted(MessageProcessedNormally, ThrottleModeEnded);
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

                var rateLimitingSettings = new RateLimitSettings(TimeSpan.FromSeconds(5), cancellation =>
                    {
                        scenarioContext.ThrottleModeEntered = true;

                        return Task.CompletedTask;
                    },
                    cancellation =>
                    {
                        scenarioContext.ThrottleModeEnded = true;
                        scenarioContext.MaybeCompleted();

                        return Task.CompletedTask;
                    });

                recoverability.OnConsecutiveFailures(2, rateLimitingSettings);
            });

        [Handler]
        public class InitiatingHandler(Context testContext) : IHandleMessages<InitiatingMessage>
        {
            public Task Handle(InitiatingMessage initiatingMessage, IMessageHandlerContext context)
            {
                if (!testContext.ThrottleModeEntered)
                {
                    throw new SimulatedException("THIS IS A MESSAGE THAT WILL NEVER SUCCEED");
                }

                testContext.MessageProcessedNormally = true;
                testContext.MaybeCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class InitiatingMessage : IMessage
    {
        public Guid Id { get; set; }
    }
}