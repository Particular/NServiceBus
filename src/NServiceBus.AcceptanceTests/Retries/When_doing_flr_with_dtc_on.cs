namespace NServiceBus.AcceptanceTests.Retries
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Config;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_doing_flr_with_dtc_on : NServiceBusAcceptanceTest
    {
        public const int maxretries = 5;
            
        [Test]
        public void Should_do_X_retries_by_default_with_dtc_on()
        {
            Scenario.Define(() => new Context { Id = Guid.NewGuid() })
                    .WithEndpoint<RetryEndpoint>(b => b.Given((bus, context) => bus.SendLocal(new MessageToBeRetried{ Id = context.Id })))
                    .AllowExceptions()
                    .Done(c => c.GaveUpOnRetries || c.NumberOfTimesInvoked > maxretries)
                    .Repeat(r => r.For<AllDtcTransports>())
                    //we add 1 since first call + X retries totals to X+1
                    .Should(c => Assert.AreEqual(maxretries+1, c.NumberOfTimesInvoked, string.Format("The FLR should by default retry {0} times", maxretries)))
                    .Run();

        }


        public class Context : ScenarioContext
        {
            public Guid Id { get; set; }

            public int NumberOfTimesInvoked { get; set; }

            public bool GaveUpOnRetries { get; set; }
        }

        public class RetryEndpoint : EndpointConfigurationBuilder
        {
            public RetryEndpoint()
            {
                EndpointSetup<DefaultServer>()
                    .WithConfig<TransportConfig>(c => c.MaxRetries = maxretries);
            }

            class ErrorNotificationSpy : IWantToRunWhenBusStartsAndStops
            {
                public Context Context { get; set; }

                public BusNotifications BusNotifications { get; set; }

                public void Start()
                {
                    BusNotifications.Errors.MessageSentToErrorQueue.Subscribe(e =>
                    {
                        Context.GaveUpOnRetries = true;
                    });
                }

                public void Stop() { }
            }

            class MessageToBeRetriedHandler : IHandleMessages<MessageToBeRetried>
            {
                public Context Context { get; set; }

                public void Handle(MessageToBeRetried message)
                {
                    if (message.Id != Context.Id) return; // messages from previous test runs must be ignored

                    Context.NumberOfTimesInvoked++;

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