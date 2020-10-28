namespace NServiceBus.AcceptanceTests.Core.Recoverability
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using Logging;
    using NUnit.Framework;

    public class When_subscribing_to_error_notifications : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_retain_exception_details_over_immediate_and_delayed_retries()
        {
            Requires.DelayedDelivery();

            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                .WithEndpoint<DelayedRetriesEndpoint>(b =>
                {
                    b.DoNotFailOnErrorMessages();
                    b.When((session, c) => session.SendLocal(new MessageToBeRetried
                    {
                        Id = c.Id
                    }));
                })
                .Done(c => c.MessageSentToError)
                .Run();

            Assert.IsInstanceOf<SimulatedException>(context.MessageSentToErrorException);
            Assert.True(context.Logs.Any(l => l.Level == LogLevel.Error && l.Message.Contains("Simulated exception message")), "The last exception should be logged as `error` before sending it to the error queue");

            // Immediate Retries max retries = 3 means we will be processing 4 times. Delayed Retries max retries = 2 means we will do 3 * Immediate Retries
            Assert.AreEqual(4 * 3, context.TotalNumberOfHandlerInvocations);
            Assert.AreEqual(3 * 3, context.TotalNumberOfImmediateRetriesEventInvocations);
            Assert.AreEqual(2, context.NumberOfDelayedRetriesPerformed);
        }

        class Context : ScenarioContext
        {
            public Guid Id { get; set; }
            public int TotalNumberOfImmediateRetriesEventInvocations { get; set; }
            public int TotalNumberOfHandlerInvocations { get; set; }
            public int NumberOfDelayedRetriesPerformed { get; set; }
            public bool MessageSentToError { get; set; }
            public Exception MessageSentToErrorException { get; set; }
        }

        public class DelayedRetriesEndpoint : EndpointConfigurationBuilder
        {
            public DelayedRetriesEndpoint()
            {
                EndpointSetup<DefaultServer>((config, context) =>
                {
                    var testContext = (Context)context.ScenarioContext;

                    var recoverability = config.Recoverability();
                    recoverability.Failed(f => f.OnMessageSentToErrorQueue(failedMessage =>
                    {
                        testContext.MessageSentToErrorException = failedMessage.Exception;
                        testContext.MessageSentToError = true;
                        return Task.FromResult(0);
                    }));
                    recoverability.Delayed(settings =>
                    {
                        settings.NumberOfRetries(2);
                        settings.TimeIncrease(TimeSpan.FromMilliseconds(1));
                        settings.OnMessageBeingRetried(retry =>
                        {
                            testContext.NumberOfDelayedRetriesPerformed++;
                            return Task.FromResult(0);
                        });
                    });
                    recoverability.Immediate(settings =>
                    {
                        settings.NumberOfRetries(3);
                        settings.OnMessageBeingRetried(retry =>
                        {
                            testContext.TotalNumberOfImmediateRetriesEventInvocations++;
                            return Task.FromResult(0);
                        });
                    });
                });
            }

            class MessageToBeRetriedHandler : IHandleMessages<MessageToBeRetried>
            {
                public MessageToBeRetriedHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(MessageToBeRetried message, IMessageHandlerContext context)
                {
                    if (message.Id != testContext.Id)
                    {
                        return Task.FromResult(0); // messages from previous test runs must be ignored
                    }

                    testContext.TotalNumberOfHandlerInvocations++;

                    throw new SimulatedException("Simulated exception message");
                }

                Context testContext;
            }
        }


        public class MessageToBeRetried : IMessage
        {
            public Guid Id { get; set; }
        }
    }
}