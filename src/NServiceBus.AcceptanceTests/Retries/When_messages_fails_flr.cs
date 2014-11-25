namespace NServiceBus.AcceptanceTests.Retries
{
    using System;
    using Config;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_messages_fails_flr : NServiceBusAcceptanceTest
    {
        static TimeSpan SlrDelay = TimeSpan.FromSeconds(5);

        [Test]
        public void Should_be_moved_to_slr()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<SLREndpoint>(b => b.Given(bus => bus.SendLocal(new MessageToBeRetried())))
                    .Done(c => c.NumberOfTimesInvoked >= 2)
                    .Repeat(r => r.For(Transports.Default))
                    .Should(context =>
                        {
                            Assert.GreaterOrEqual(1,context.NumberOfSlrRetriesPerformed, "The SLR should only do one retry");
                            Assert.GreaterOrEqual(context.TimeOfSecondAttempt - context.TimeOfFirstAttempt,SlrDelay , "The SLR should delay the retry");
                        })
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public int NumberOfTimesInvoked { get; set; }

            public DateTime TimeOfFirstAttempt { get; set; }
            public DateTime TimeOfSecondAttempt { get; set; }

            public int NumberOfSlrRetriesPerformed { get; set; }
        }

        public class SLREndpoint : EndpointConfigurationBuilder
        {
            public SLREndpoint()
            {
                EndpointSetup<DefaultServer>()
                    .WithConfig<TransportConfig>(c =>
                        {
                            c.MaxRetries = 0; //to skip the FLR
                        })
                        .WithConfig<SecondLevelRetriesConfig>(c =>
                        {
                            c.NumberOfRetries = 1;
                            c.TimeIncrease = SlrDelay;
                        })
                        .AllowExceptions();
            }


            class MessageToBeRetriedHandler:IHandleMessages<MessageToBeRetried>
            {
                public Context Context { get; set; }

                public IBus Bus { get; set; }

                public void Handle(MessageToBeRetried message)
                {
                    Context.NumberOfTimesInvoked++;

                    if (Context.NumberOfTimesInvoked == 1)
                        Context.TimeOfFirstAttempt = DateTime.UtcNow;

                    if (Context.NumberOfTimesInvoked == 2)
                    {
                        Context.TimeOfSecondAttempt = DateTime.UtcNow;
                    }

                    Context.NumberOfSlrRetriesPerformed = int.Parse(Bus.CurrentMessageContext.Headers[Headers.Retries]); 
                        
                    throw new Exception("Simulated exception");
                }
            }
        }

        [Serializable]
        public class MessageToBeRetried : IMessage
        {
        }
    }


}