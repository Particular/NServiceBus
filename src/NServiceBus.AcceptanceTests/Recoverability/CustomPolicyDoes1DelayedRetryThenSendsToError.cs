namespace NServiceBus.AcceptanceTests.Recoverability;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Support;
using EndpointTemplates;
using NUnit.Framework;
using Transport;

public class CustomPolicyDoes1DelayedRetryThenSendsToError : NServiceBusAcceptanceTest
{
    [Test]
    public void Should_execute_twice_and_send_to_error_queue()
    {
        Requires.DelayedDelivery();

        var messageId = Guid.NewGuid().ToString();
        var exception = Assert.ThrowsAsync<MessageFailedException>(async () => await Scenario.Define<Context>()
            .WithEndpoint<RetryEndpoint>(b => b
                .When(bus =>
                {
                    var sendOptions = new SendOptions();
                    sendOptions.RouteToThisEndpoint();
                    sendOptions.SetMessageId(messageId);
                    return bus.Send(new MessageToBeRetried(), sendOptions);
                }))
            .Run());

        var context = (Context)exception.ScenarioContext;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Count, Is.EqualTo(2));
            Assert.That(exception.FailedMessage.MessageId, Is.EqualTo(messageId));
        }
    }

    public class Context : ScenarioContext
    {
        public int Count { get; set; }
    }

    public class RetryEndpoint : EndpointConfigurationBuilder
    {
        public RetryEndpoint() =>
            EndpointSetup<DefaultServer>((configure, context) =>
            {
                configure.Recoverability()
                    .CustomPolicy(RetryPolicy);
            });

        RecoverabilityAction RetryPolicy(RecoverabilityConfig config, ErrorContext context)
        {
            if (context.DelayedDeliveriesPerformed == 0)
            {
                return RecoverabilityAction.DelayedRetry(TimeSpan.FromMilliseconds(10));
            }

            return RecoverabilityAction.MoveToError(config.Failed.ErrorQueue);
        }

        [Handler]
        public class MessageToBeRetriedHandler(Context testContext) : IHandleMessages<MessageToBeRetried>
        {
            public Task Handle(MessageToBeRetried message, IMessageHandlerContext context)
            {
                testContext.Count++;
                throw new SimulatedException();
            }
        }
    }

    public class MessageToBeRetried : IMessage;
}