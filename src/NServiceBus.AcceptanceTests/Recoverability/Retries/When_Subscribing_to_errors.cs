namespace NServiceBus.AcceptanceTests.Recoverability.Retries
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class When_Subscribing_to_errors : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_retain_exception_details_over_FLR_and_SLR()
        {
            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                .WithEndpoint<SLREndpoint>(b => b
                    .DoNotFailOnErrorMessages())
                .Done(c => c.MessageSentToError)
                .Run();

            Assert.IsInstanceOf<SimulatedException>(context.MessageSentToErrorException);
            Assert.True(context.Logs.Any(l => l.Level == "error" && l.Message.Contains("Simulated exception message")), "The last exception should be logged as `error` before sending it to the error queue");

            //FLR max retries = 3 means we will be processing 4 times. SLR max retries = 2 means we will do 3*FLR
            Assert.AreEqual(4*3, context.TotalNumberOfFLRTimesInvokedInHandler);
            Assert.AreEqual(4*3, context.TotalNumberOfFLRTimesInvoked);
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
                EndpointSetup<DefaultServer>(config =>
                {
                    config.EnableFeature<SecondLevelRetries>();
                    config.EnableFeature<TimeoutManager>();
                    config.EnableFeature<FirstLevelRetries>();
                })
                    .WithConfig<TransportConfig>(c => { c.MaxRetries = 3; })
                    .WithConfig<SecondLevelRetriesConfig>(c =>
                    {
                        c.NumberOfRetries = 2;
                        c.TimeIncrease = TimeSpan.FromMilliseconds(1);
                    });
            }


            class MessageToBeRetriedHandler : IHandleMessages<MessageToBeRetried>
            {
                public Context Context { get; set; }

                public Task Handle(MessageToBeRetried message, IMessageHandlerContext context)
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

            public Task Start(IBusSession session)
            {
                Notifications.Errors.MessageSentToErrorQueue += (sender, message) =>
                {
                    Context.MessageSentToErrorException = message.Exception;
                    Context.MessageSentToError = true;
                };

                Notifications.Errors.MessageHasFailedAFirstLevelRetryAttempt += (sender, retry) => Context.TotalNumberOfFLRTimesInvoked++;
                Notifications.Errors.MessageHasBeenSentToSecondLevelRetries += (sender, retry) => Context.NumberOfSLRRetriesPerformed++;

                return session.SendLocal(new MessageToBeRetried
                {
                    Id = Context.Id
                });
            }

            public Task Stop(IBusSession session)
            {
                return Task.FromResult(0);
            }
        }
    }
}