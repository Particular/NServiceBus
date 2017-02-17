﻿namespace NServiceBus.AcceptanceTests.Recoverability.Retries
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    public class when_immediate_retries_fail : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_do_delayed_retries()
        {
            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                .WithEndpoint<DelayedRetryEndpoint>(b => b
                    .When((session, ctx) => session.SendLocal(new MessageToBeRetried
                    {
                        Id = ctx.Id
                    }))
                    .DoNotFailOnErrorMessages())
                .Done(c => c.NumberOfTimesInvoked >= 2)

                .Run();

            Assert.GreaterOrEqual(1, context.NumberOfDelayedRetriesPerformed, "Should only do one retry");
            Assert.GreaterOrEqual(context.TimeOfSecondAttempt - context.TimeOfFirstAttempt, Delay, "Should delay the retry");
        }

        static TimeSpan Delay = TimeSpan.FromMilliseconds(1);

        class Context : ScenarioContext
        {
            public Guid Id { get; set; }

            public int NumberOfTimesInvoked { get; set; }

            public DateTime TimeOfFirstAttempt { get; set; }
            public DateTime TimeOfSecondAttempt { get; set; }

            public int NumberOfDelayedRetriesPerformed { get; set; }
        }

        public class DelayedRetryEndpoint : EndpointConfigurationBuilder
        {
            public DelayedRetryEndpoint()
            {
                EndpointSetup<DefaultServer>(config =>
                {
                    config.EnableFeature<TimeoutManager>();
                    config.Recoverability().Delayed(settings =>
                    {
                        settings.NumberOfRetries(1);
                        settings.TimeIncrease(Delay);
                    });
                });
            }

            class MessageToBeRetriedHandler : IHandleMessages<MessageToBeRetried>
            {
                public Context TestContext { get; set; }

                public Task Handle(MessageToBeRetried message, IMessageHandlerContext context)
                {
                    if (message.Id != TestContext.Id)
                    {
                        return Task.FromResult(0); // messages from previous test runs must be ignored
                    }

                    TestContext.NumberOfTimesInvoked++;

                    if (TestContext.NumberOfTimesInvoked == 1)
                    {
                        TestContext.TimeOfFirstAttempt = DateTime.UtcNow;
                    }

                    if (TestContext.NumberOfTimesInvoked == 2)
                    {
                        TestContext.TimeOfSecondAttempt = DateTime.UtcNow;
                    }

                    string retries;

                    if (context.MessageHeaders.TryGetValue(Headers.DelayedRetries, out retries))
                    {
                        TestContext.NumberOfDelayedRetriesPerformed = int.Parse(retries);
                    }

                    throw new SimulatedException();
                }
            }
        }

        public class MessageToBeRetried : IMessage
        {
            public Guid Id { get; set; }
        }
    }
}