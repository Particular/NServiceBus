namespace NServiceBus.AcceptanceTests.Core.Recoverability
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_messages_never_succeed : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_throttle_pipeline_after_configured_number_of_consecutive_failures()
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
                .Done(c => Context.ThrottleModeEntered)
                .Run();
        }

        class Context : ScenarioContext
        {
            public static bool ThrottleModeEntered { get; set; }
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
                        d.NumberOfConsecutiveFailuresBeforeThrottling(2);
                        d.ThrottledModeConcurrency(1);
                        d.TimeToWaitBeforeThrottledProcessingAttempts(TimeSpan.FromSeconds(5));

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