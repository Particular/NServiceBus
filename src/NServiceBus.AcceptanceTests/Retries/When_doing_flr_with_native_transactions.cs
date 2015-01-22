namespace NServiceBus.AcceptanceTests.Retries
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_doing_flr_with_native_transactions : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_do_5_retries_by_default_with_native_transactions()
        {
            Scenario.Define(() => new Context { Id = Guid.NewGuid() })
                    .WithEndpoint<RetryEndpoint>(b => b.Given((bus, context) => bus.SendLocal(new MessageToBeRetried { Id = context.Id })))
                    .AllowExceptions()
                    .Done(c => c.ForwardedToErrorQueue || c.NumberOfTimesInvoked > 5)
                    .Repeat(r => r.For(Transports.Default))
                    .Should(c => Assert.AreEqual(5+1, c.NumberOfTimesInvoked, "The FLR should by default retry 5 times"))
                    .Run();

        }

        public class Context : ScenarioContext
        {
            public Guid Id { get; set; }

            public int NumberOfTimesInvoked { get; set; }

            public bool ForwardedToErrorQueue { get; set; }
        }

        public class RetryEndpoint : EndpointConfigurationBuilder
        {
            public RetryEndpoint()
            {
                EndpointSetup<DefaultServer>(b =>
                {
                    b.Transactions().DisableDistributedTransactions();
                    b.DisableFeature<Features.SecondLevelRetries>();
                });
            }

            class ErrorNotificationSpy : IWantToRunWhenBusStartsAndStops
            {
                public Context Context { get; set; }

                public BusNotifications BusNotifications { get; set; }

                public void Start()
                {
                    BusNotifications.Errors.MessageSentToErrorQueue.Subscribe(e =>
                    {
                        Context.ForwardedToErrorQueue = true;
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

        public class MessageToBeRetried : IMessage
        {
            public Guid Id { get; set; }
        }
    }


}