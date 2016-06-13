﻿namespace NServiceBus.AcceptanceTests.Recoverability.Retries
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NServiceBus.Config;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_fails_flr : NServiceBusAcceptanceTest
    {
        [Test]
        public Task Should_be_moved_to_slr()
        {
            return Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                .WithEndpoint<SLREndpoint>(b => b
                    .When((session, context) => session.SendLocal(new MessageToBeRetried
                    {
                        Id = context.Id
                    }))
                    .DoNotFailOnErrorMessages())
                .Done(c => c.NumberOfTimesInvoked >= 2)
                .Repeat(r => r.For(Transports.Default))
                .Should(context =>
                {
                    Assert.GreaterOrEqual(1, context.NumberOfSlrRetriesPerformed, "The SLR should only do one retry");
                    Assert.GreaterOrEqual(context.TimeOfSecondAttempt - context.TimeOfFirstAttempt, SlrDelay, "The SLR should delay the retry");
                })
                .Run();
        }

        static TimeSpan SlrDelay = TimeSpan.FromMilliseconds(1);

        class Context : ScenarioContext
        {
            public Guid Id { get; set; }

            public int NumberOfTimesInvoked { get; set; }

            public DateTime TimeOfFirstAttempt { get; set; }
            public DateTime TimeOfSecondAttempt { get; set; }

            public int NumberOfSlrRetriesPerformed { get; set; }
        }

        public class SLREndpoint : EndpointConfigurationBuilder
        {
            public SLREndpoint()
            {
                EndpointSetup<DefaultServer>(config =>
                {
                    config.EnableFeature<TimeoutManager>();
                })
                    .WithConfig<SecondLevelRetriesConfig>(c =>
                    {
                        c.NumberOfRetries = 1;
                        c.TimeIncrease = SlrDelay;
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

                    if (context.MessageHeaders.TryGetValue(Headers.Retries, out retries))
                    {
                        TestContext.NumberOfSlrRetriesPerformed = int.Parse(retries);
                    }

                    throw new SimulatedException();
                }
            }
        }

        [Serializable]
        public class MessageToBeRetried : IMessage
        {
            public Guid Id { get; set; }
        }
    }
}