namespace NServiceBus.AcceptanceTests.Core.Recoverability;

using System;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
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

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.MessageSentToErrorException, Is.InstanceOf<SimulatedException>());
            Assert.That(context.Logs.Any(l => l.Level == LogLevel.Error && l.Message.Contains("Simulated exception message")), Is.True, "The last exception should be logged as `error` before sending it to the error queue");

            // Immediate Retries max retries = 3 means we will be processing 4 times. Delayed Retries max retries = 2 means we will do 3 * Immediate Retries
            Assert.That(context.TotalNumberOfHandlerInvocations, Is.EqualTo(4 * 3));
            Assert.That(context.TotalNumberOfImmediateRetriesEventInvocations, Is.EqualTo(3 * 3));
            Assert.That(context.NumberOfDelayedRetriesPerformed, Is.EqualTo(2));
        }
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
                recoverability.Failed(f => f.OnMessageSentToErrorQueue((failedMessage, _) =>
                {
                    testContext.MessageSentToErrorException = failedMessage.Exception;
                    testContext.MessageSentToError = true;
                    return Task.CompletedTask;
                }));
                recoverability.Delayed(settings =>
                {
                    settings.NumberOfRetries(2);
                    settings.TimeIncrease(TimeSpan.FromMilliseconds(1));
                    settings.OnMessageBeingRetried((retry, _) =>
                    {
                        testContext.NumberOfDelayedRetriesPerformed++;
                        return Task.CompletedTask;
                    });
                });
                recoverability.Immediate(settings =>
                {
                    settings.NumberOfRetries(3);
                    settings.OnMessageBeingRetried((retry, _) =>
                    {
                        testContext.TotalNumberOfImmediateRetriesEventInvocations++;
                        return Task.CompletedTask;
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
                    return Task.CompletedTask; // messages from previous test runs must be ignored
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