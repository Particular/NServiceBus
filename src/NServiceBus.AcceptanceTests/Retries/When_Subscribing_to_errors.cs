namespace NServiceBus.AcceptanceTests.Retries
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Config;
    using NUnit.Framework;

    public class When_Subscribing_to_errors : NServiceBusAcceptanceTest
    {
        static TimeSpan SlrDelay = TimeSpan.FromSeconds(1);

        [Test]
        public void Should_receive_notifications()
        {
            var context = new Context
            {
                Id = Guid.NewGuid()
            };
            Scenario.Define(context)
                    .WithEndpoint<SLREndpoint>(b => b.Given((bus, c) => bus.SendLocal(new MessageToBeRetried { Id = c.Id })))
                    .AllowExceptions(e => e.Message.Contains("Simulated exception"))
                    .Done(c => c.MessageSentToError)
                    .Run();

            Assert.AreEqual(3 * 3, context.TotalNumberOfFLRTimesInvokedInHandler);
            Assert.AreEqual(3 * 3, context.TotalNumberOfFLRTimesInvoked);
            Assert.AreEqual(3, context.NumberOfSLRRetriesPerformed);
        }

        public class Context : ScenarioContext
        {
            public Guid Id { get; set; }
            public int TotalNumberOfFLRTimesInvoked { get; set; }
            public int TotalNumberOfFLRTimesInvokedInHandler { get; set; }
            public int NumberOfSLRRetriesPerformed { get; set; }
            public bool MessageSentToError { get; set; }
        }

        public class SLREndpoint : EndpointConfigurationBuilder
        {
            public SLREndpoint()
            {
                EndpointSetup<DefaultServer>()
                    .WithConfig<TransportConfig>(c =>
                        {
                            c.MaxRetries = 3;
                        })
                        .WithConfig<SecondLevelRetriesConfig>(c =>
                        {
                            c.NumberOfRetries = 2;
                            c.TimeIncrease = SlrDelay;
                        });
            }


            class MessageToBeRetriedHandler:IHandleMessages<MessageToBeRetried>
            {
                public Context Context { get; set; }

                public void Handle(MessageToBeRetried message)
                {
                    if (message.Id != Context.Id) return; // ignore messages from previous test runs

                    Context.TotalNumberOfFLRTimesInvokedInHandler++;

                    throw new Exception("Simulated exception");
                }
            }
        }

        [Serializable]
        public class MessageToBeRetried : IMessage
        {
            public Guid Id { get; set; }
        }

        public class MyErrorSubscriber : IWantToRunWhenBusStartsAndStops
        {
            public Context Context { get; set; }

            public BusNotifications Notifications { get; set; }

            public void Start()
            {
                Notifications.Errors.MessageSentToErrorQueue.Subscribe(message => Context.MessageSentToError = true);
                Notifications.Errors.MessageHasFailedAFirstLevelRetryAttempt.Subscribe(message => Context.TotalNumberOfFLRTimesInvoked++);
                Notifications.Errors.MessageHasBeenSentToSecondLevelRetries.Subscribe(message => Context.NumberOfSLRRetriesPerformed++);
            }

            public void Stop()
            {
            }
        }
    }
}