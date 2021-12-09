namespace NServiceBus.AcceptanceTests.Core.Recoverability
{
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
                .Done(c => Context.MessageProcessedNormally && Context.ThrottleModeEnded)
                .Run();
        }

        class Context : ScenarioContext
        {
            public static bool ThrottleModeEntered { get; set; }
            public static bool ThrottleModeEnded { get; set; }
            public static bool MessageProcessedNormally { get; set; }
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

                    var rateLimitingSettings = new RateLimitSettings(TimeSpan.FromSeconds(5), cancellation =>
                    {
                        Context.ThrottleModeEntered = true;

                        return Task.FromResult(0);
                    },
                    cancellation =>
                    {
                        Context.ThrottleModeEnded = true;

                        return Task.FromResult(0);
                    });

                    recoverability.OnConsecutiveFailures(2, rateLimitingSettings);
                });
            }

            class InitiatingHandler : IHandleMessages<InitiatingMessage>
            {
                public Task Handle(InitiatingMessage initiatingMessage, IMessageHandlerContext context)
                {
                    if (!Context.ThrottleModeEntered)
                    {
                        throw new SimulatedException("THIS IS A MESSAGE THAT WILL NEVER SUCCEED");
                    }

                    Context.MessageProcessedNormally = true;

                    return Task.FromResult(0);
                }
            }
        }

        public class InitiatingMessage : IMessage
        {
            public Guid Id { get; set; }
        }
    }
}