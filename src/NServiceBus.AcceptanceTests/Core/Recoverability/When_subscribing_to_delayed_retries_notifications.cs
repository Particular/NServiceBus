﻿namespace NServiceBus.AcceptanceTests.Core.Recoverability
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Faults;
    using NUnit.Framework;

    public class When_subscribing_to_delayed_retries_notifications : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_trigger_notification_on_delayed_retry()
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

            Assert.IsInstanceOf<SimulatedException>(context.LastDelayedRetryInfo?.Exception);
            // Immediate Retries max retries = 3 means we will be processing 4 times. Delayed Retries max retries = 2 means we will do 3 * Immediate Retries
            Assert.AreEqual(4 * 3, context.TotalNumberOfHandlerInvocations);
            Assert.AreEqual(2, context.NumberOfDelayedRetriesPerformed);
            Assert.AreEqual(2, context.LastDelayedRetryInfo.RetryAttempt);
        }

        class Context : ScenarioContext
        {
            public Guid Id { get; set; }
            public int TotalNumberOfImmediateRetriesEventInvocations { get; set; }
            public int TotalNumberOfHandlerInvocations { get; set; }
            public int NumberOfDelayedRetriesPerformed { get; set; }
            public bool MessageSentToError { get; set; }
            public DelayedRetryMessage LastDelayedRetryInfo { get; set; }
        }

        public class DelayedRetriesEndpoint : EndpointConfigurationBuilder
        {
            public DelayedRetriesEndpoint()
            {
                EndpointSetup<DefaultServer>((config, context) =>
                {
                    var testContext = (Context)context.ScenarioContext;

                    var recoverability = config.Recoverability();
                    recoverability.Failed(f => f.OnMessageSentToErrorQueue((failedMessage, _) =>
                    {
                        testContext.MessageSentToError = true;
                        return Task.FromResult(0);
                    }));
                    recoverability.Immediate(settings =>
                    {
                        settings.NumberOfRetries(3);
                        settings.OnMessageBeingRetried((retry, _) =>
                        {
                            testContext.TotalNumberOfImmediateRetriesEventInvocations++;
                            return Task.FromResult(0);
                        });
                    });
                    recoverability.Delayed(settings =>
                    {
                        settings.NumberOfRetries(2);
                        settings.TimeIncrease(TimeSpan.FromMilliseconds(1));
                        settings.OnMessageBeingRetried((retry, _) =>
                        {
                            testContext.NumberOfDelayedRetriesPerformed++;
                            testContext.LastDelayedRetryInfo = retry;
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