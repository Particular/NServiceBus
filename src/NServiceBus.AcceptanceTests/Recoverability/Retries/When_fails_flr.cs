namespace NServiceBus.AcceptanceTests.Recoverability.Retries
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NServiceBus.Config;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class When_fails_flr : NServiceBusAcceptanceTest
    {
        static TimeSpan SlrDelay = TimeSpan.FromSeconds(5);

        [Test]
        public async Task Should_be_moved_to_slr()
        {
            await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                    .WithEndpoint<SLREndpoint>(b => b.Given((bus, context) => bus.SendLocalAsync(new MessageToBeRetried { Id = context.Id })))
                    .AllowSimulatedExceptions()
                    .Done(c => c.NumberOfTimesInvoked >= 2)
                    .Repeat(r => r.For(Transports.Default))
                    .Should(context =>
                        {
                            Assert.GreaterOrEqual(1, context.NumberOfSlrRetriesPerformed, "The SLR should only do one retry");
                            Assert.GreaterOrEqual(context.TimeOfSecondAttempt - context.TimeOfFirstAttempt, SlrDelay, "The SLR should delay the retry");
                        })
                    .Run();
        }

        public class Context : ScenarioContext
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
                    config.EnableFeature<SecondLevelRetries>();
                })
                    .WithConfig<TransportConfig>(c =>
                    {
                        c.MaxRetries = 0; //to skip the FLR
                    })
                    .WithConfig<SecondLevelRetriesConfig>(c =>
                    {
                        c.NumberOfRetries = 1;
                        c.TimeIncrease = SlrDelay;
                    });
            }


            class MessageToBeRetriedHandler : IHandleMessages<MessageToBeRetried>
            {
                public Context Context { get; set; }

                public IBus Bus { get; set; }

                public Task Handle(MessageToBeRetried message)
                {
                    if (message.Id != Context.Id)
                        return Task.FromResult(0); // messages from previous test runs must be ignored

                    Context.NumberOfTimesInvoked++;

                    if (Context.NumberOfTimesInvoked == 1)
                        Context.TimeOfFirstAttempt = DateTime.UtcNow;

                    if (Context.NumberOfTimesInvoked == 2)
                    {
                        Context.TimeOfSecondAttempt = DateTime.UtcNow;
                    }

                    string retries;

                    if (Bus.CurrentMessageContext.Headers.TryGetValue(Headers.Retries, out retries))
                    {
                        Context.NumberOfSlrRetriesPerformed = int.Parse(retries);
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