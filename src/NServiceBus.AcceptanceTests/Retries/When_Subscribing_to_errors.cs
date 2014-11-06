namespace NServiceBus.AcceptanceTests.Retries
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NUnit.Framework;

    public class When_Subscribing_to_errors : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_contain_exception_details()
        {
            var context = new Context
            {
                Id = Guid.NewGuid()
            };
            Scenario.Define(context)
                .WithEndpoint<SLREndpoint>(b => b.Given((bus, c) => bus.SendLocal(new MessageToBeRetried
                {
                    Id = c.Id
                })))
                .AllowExceptions(e => e.Message.Contains("Simulated exception"))
                .Done(c => c.MessageSentToError)
                .Run(TimeSpan.FromMinutes(5));

            Assert.IsInstanceOf<MySpecialException>(context.MessageSentToErrorException);
        }

        [Test]
        public void Should_receive_notifications()
        {
            var context = new Context
            {
                Id = Guid.NewGuid()
            };
            Scenario.Define(context)
                .WithEndpoint<SLREndpoint>()
                .AllowExceptions(e => e.Message.Contains("Simulated exception"))
                .Done(c => c.MessageSentToError)
                .Run(TimeSpan.FromMinutes(5));

            Assert.AreEqual(3*3, context.TotalNumberOfFLRTimesInvokedInHandler);
            Assert.AreEqual(3*3, context.TotalNumberOfFLRTimesInvoked);
            Assert.AreEqual(2, context.NumberOfSLRRetriesPerformed);
        }

        public class Context : ScenarioContext
        {
            public Guid Id { get; set; }
            public int TotalNumberOfFLRTimesInvoked { get; set; }
            public int TotalNumberOfFLRTimesInvokedInHandler { get; set; }
            public int NumberOfSLRRetriesPerformed { get; set; }
            public bool MessageSentToError { get; set; }
            public Exception MessageSentToErrorException { get; set; }
        }

        public class SLREndpoint : EndpointConfigurationBuilder
        {
            public SLREndpoint()
            {
                EndpointSetup<DefaultServer>()
                    .WithConfig<TransportConfig>(c => { c.MaxRetries = 3; })
                    .WithConfig<SecondLevelRetriesConfig>(c =>
                    {
                        c.NumberOfRetries = 2;
                        c.TimeIncrease = TimeSpan.FromSeconds(1);
                    });
            }


            class MessageToBeRetriedHandler : IHandleMessages<MessageToBeRetried>
            {
                public Context Context { get; set; }

                public void Handle(MessageToBeRetried message)
                {
                    if (message.Id != Context.Id)
                    {
                        return; // ignore messages from previous test runs
                    }

                    Context.TotalNumberOfFLRTimesInvokedInHandler++;

                    throw new MySpecialException();
                }
            }
        }

        public class MySpecialException : Exception
        {
            public MySpecialException()
                : base("Simulated exception")
            {
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

            public IBus Bus { get; set; }

            public void Start()
            {
                unsubscribeStreams.Add(Notifications.Errors.MessageSentToErrorQueue.Subscribe(message =>
                {
                    Context.MessageSentToErrorException = message.Exception;
                    Context.MessageSentToError = true;
                }));
                unsubscribeStreams.Add(Notifications.Errors.MessageHasFailedAFirstLevelRetryAttempt.Subscribe(message => Context.TotalNumberOfFLRTimesInvoked++));
                unsubscribeStreams.Add(Notifications.Errors.MessageHasBeenSentToSecondLevelRetries.Subscribe(message => Context.NumberOfSLRRetriesPerformed++));

                Bus.SendLocal(new MessageToBeRetried
                {
                    Id = Context.Id
                });
            }

            public void Stop()
            {
                foreach (var unsubscribeStream in unsubscribeStreams)
                {
                    unsubscribeStream.Dispose();
                }
            }

            List<IDisposable> unsubscribeStreams = new List<IDisposable>();
        }
    }
}