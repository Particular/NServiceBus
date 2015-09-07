namespace NServiceBus.AcceptanceTests.Recoverability.Retries
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NServiceBus.Config;
    using NUnit.Framework;

    public class When_doing_flr_with_dtc_on : NServiceBusAcceptanceTest
    {
        const int maxretries = 4;

        [Test]
        public async Task Should_do_X_retries_by_default_with_dtc_on()
        {
            await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                   .WithEndpoint<RetryEndpoint>(b => b.Given((bus, context) =>
                   {
                       bus.SendLocal(new MessageToBeRetried { Id = context.Id });
                       return Task.FromResult(0);
                   }))
                   .AllowExceptions()
                   .Done(c => c.GaveUpOnRetries)
                   .Repeat(r => r.For<AllDtcTransports>())
                   .Should(c =>
                   {
                        //we add 1 since first call + X retries totals to X+1
                        Assert.AreEqual(maxretries + 1, c.NumberOfTimesInvoked, string.Format("The FLR should by default retry {0} times", maxretries));
                       Assert.AreEqual(maxretries, c.Logs.Count(l => l.Message
                           .StartsWith(string.Format("First Level Retry is going to retry message '{0}' because of an exception:", c.PhysicalMessageId))));
                       Assert.AreEqual(1, c.Logs.Count(l => l.Message
                           .StartsWith(string.Format("Giving up First Level Retries for message '{0}'.", c.PhysicalMessageId))));
                   })
                   .Run();
        }

        public class Context : ScenarioContext
        {
            public Guid Id { get; set; }

            public int NumberOfTimesInvoked { get; set; }

            public bool GaveUpOnRetries { get; set; }

            public string PhysicalMessageId { get; set; }
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
                public IBus Bus { get; set; }

                public Context Context { get; set; }

                public void Handle(MessageToBeRetried message)
                {
                    if (message.Id != Context.Id) return; // messages from previous test runs must be ignored

                    Context.PhysicalMessageId = Bus.CurrentMessageContext.Id;
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