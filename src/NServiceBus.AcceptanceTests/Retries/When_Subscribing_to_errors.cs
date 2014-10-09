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

            Assert.AreEqual(2 * 3, context.TotalNumberOfFLRTimesInvokedInHandler);
            Assert.AreEqual(2 * 3, context.TotalNumberOfFLRTimesInvoked);
            Assert.AreEqual(2, context.NumberOfSLRRetriesPerformed);
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
                EndpointSetup<DefaultServer>(config => config.RegisterErrorSubscriber<MyErrorSubscriber>())
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

        public class MyErrorSubscriber : IErrorSubscriber
        {
            public Context Context { get; set; }

            public void MessageHasBeenSentToErrorQueue(TransportMessage message, Exception exception)
            {
                Context.MessageSentToError = true;
            }

            public void MessageHasFailedAFirstLevelRetryAttempt(int firstLevelRetryAttempt, TransportMessage message, Exception exception)
            {
                Context.TotalNumberOfFLRTimesInvoked++;
            }

            public void MessageHasBeenSentToSecondLevelRetries(int secondLevelRetryAttempt, TransportMessage message, Exception exception)
            {
                Context.NumberOfSLRRetriesPerformed++;
            }
        }
    }
}