namespace NServiceBus.AcceptanceTests.Core.Recoverability;

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
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.LastDelayedRetryInfo?.Exception, Is.InstanceOf<SimulatedException>());
            // Immediate Retries max retries = 3 means we will be processing 4 times. Delayed Retries max retries = 2 means we will do 3 * Immediate Retries
            Assert.That(context.TotalNumberOfHandlerInvocations, Is.EqualTo(4 * 3));
            Assert.That(context.NumberOfDelayedRetriesPerformed, Is.EqualTo(2));
            Assert.That(context.LastDelayedRetryInfo.RetryAttempt, Is.EqualTo(2));
        }
    }

    public class Context : ScenarioContext
    {
        public Guid Id { get; set; }
        public int TotalNumberOfImmediateRetriesEventInvocations { get; set; }
        public int TotalNumberOfHandlerInvocations { get; set; }
        public int NumberOfDelayedRetriesPerformed { get; set; }
        public DelayedRetryMessage LastDelayedRetryInfo { get; set; }
    }

    public class DelayedRetriesEndpoint : EndpointConfigurationBuilder
    {
        public DelayedRetriesEndpoint() =>
            EndpointSetup<DefaultServer>((config, context) =>
            {
                var testContext = (Context)context.ScenarioContext;

                var recoverability = config.Recoverability();
                recoverability.Failed(f => f.OnMessageSentToErrorQueue((failedMessage, _) =>
                {
                    testContext.MarkAsCompleted();
                    return Task.CompletedTask;
                }));
                recoverability.Immediate(settings =>
                {
                    settings.NumberOfRetries(3);
                    settings.OnMessageBeingRetried((_, _) =>
                    {
                        testContext.TotalNumberOfImmediateRetriesEventInvocations++;
                        return Task.CompletedTask;
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
                        return Task.CompletedTask;
                    });
                });
            });

        [Handler]
        public class MessageToBeRetriedHandler(Context testContext) : IHandleMessages<MessageToBeRetried>
        {
            public Task Handle(MessageToBeRetried message, IMessageHandlerContext context)
            {
                if (message.Id != testContext.Id)
                {
                    return Task.CompletedTask; // messages from previous test runs must be ignored
                }

                testContext.TotalNumberOfHandlerInvocations++;

                throw new SimulatedException("Simulated exception message");
            }
        }
    }

    public class MessageToBeRetried : IMessage
    {
        public Guid Id { get; set; }
    }
}