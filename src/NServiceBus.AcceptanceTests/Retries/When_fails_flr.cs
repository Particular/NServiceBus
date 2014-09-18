namespace NServiceBus.AcceptanceTests.Retries
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Config;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_fails_flr : NServiceBusAcceptanceTest
    {
        static TimeSpan SlrDelay = TimeSpan.FromSeconds(5);

        [Test]
        public void Should_be_moved_to_slr()
        {
            Scenario.Define(() => new Context { Id = Guid.NewGuid() })
                    .WithEndpoint<SLREndpoint>(b => b.Given((bus, context) => bus.SendLocal(new MessageToBeRetried { Id = context.Id })))
                    .AllowExceptions(e => e.Message.Contains("Simulated exception"))
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
                EndpointSetup<DefaultServer>()
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


            class MessageToBeRetriedHandler:IHandleMessages<MessageToBeRetried>
            {
                public Context Context { get; set; }

                public IBus Bus { get; set; }

                public void Handle(MessageToBeRetried message)
                {
                    if (message.Id != Context.Id) return; // ignore messages from previous test runs

                    Context.NumberOfTimesInvoked++;

                    if (Context.NumberOfTimesInvoked == 1)
                        Context.TimeOfFirstAttempt = DateTime.UtcNow;

                    if (Context.NumberOfTimesInvoked == 2)
                    {
                        Context.TimeOfSecondAttempt = DateTime.UtcNow;
                    }

                    string retries;

                    if (Bus.CurrentMessageContext.Headers.TryGetValue(Headers.Retries,out retries))
                    {
                        Context.NumberOfSlrRetriesPerformed = int.Parse(retries);     
                    }
                    
                        
                    throw new Exception("Simulated exception");
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