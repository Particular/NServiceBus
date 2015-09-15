namespace NServiceBus.AcceptanceTests.Recoverability.Retries
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NServiceBus.Config;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class When_Subscribing_to_errors : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_retain_exception_details_over_FLR_and_SLR()
        {
            await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                .WithEndpoint<SLREndpoint>()
                .AllowSimulatedExceptions()
                .Done(c => c.MessageSentToError)
                .Repeat(r => r.For<AllTransports>())
                .Should(c =>
                {
                    Assert.IsInstanceOf<SimulatedException>(c.MessageSentToErrorException);
                    Assert.True(c.Logs.Any(l => l.Level == "error" && l.Message.Contains("Simulated exception message")), "The last exception should be logged as `error` before sending it to the error queue");

                    //FLR max retries = 3 means we will be processing 4 times. SLR max retries = 2 means we will do 3*FLR
                    Assert.AreEqual(4 * 3, c.TotalNumberOfFLRTimesInvokedInHandler);
                    Assert.AreEqual(4 * 3, c.TotalNumberOfFLRTimesInvoked);
                    Assert.AreEqual(2, c.NumberOfSLRRetriesPerformed);
                })
                .Run();
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
                EndpointSetup<DefaultServer>(config =>
                {
                    config.EnableFeature<SecondLevelRetries>();
                    config.EnableFeature<TimeoutManager>();
                })
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

                public Task Handle(MessageToBeRetried message)
                {
                    if (message.Id != Context.Id)
                    {
                        return Task.FromResult(0); // messages from previous test runs must be ignored
                    }

                    Context.TotalNumberOfFLRTimesInvokedInHandler++;

                    throw new SimulatedException("Simulated exception message");
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

            public IBus Bus { get; set; }

            public Task StartAsync()
            {
                unsubscribeStreams.Add(Notifications.Errors.MessageSentToErrorQueue.Subscribe(message =>
                {
                    Context.MessageSentToErrorException = message.Exception;
                    Context.MessageSentToError = true;
                }));
                unsubscribeStreams.Add(Notifications.Errors.MessageHasFailedAFirstLevelRetryAttempt.Subscribe(message => Context.TotalNumberOfFLRTimesInvoked++));
                unsubscribeStreams.Add(Notifications.Errors.MessageHasBeenSentToSecondLevelRetries.Subscribe(message => Context.NumberOfSLRRetriesPerformed++));

                return Bus.SendLocalAsync(new MessageToBeRetried
                {
                    Id = Context.Id
                });
            }

            public Task StopAsync()
            {
                foreach (var unsubscribeStream in unsubscribeStreams)
                {
                    unsubscribeStream.Dispose();
                }
                return Task.FromResult(0);
            }

            List<IDisposable> unsubscribeStreams = new List<IDisposable>();
        }
    }
}