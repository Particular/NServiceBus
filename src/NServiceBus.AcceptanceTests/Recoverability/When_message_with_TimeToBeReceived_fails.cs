namespace NServiceBus.AcceptanceTests.Recoverability;

using System;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using NUnit.Framework;

public class When_message_with_TimeToBeReceived_fails : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_not_honor_TimeToBeReceived_for_error_message()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointThatThrows>(b => b
                .When(session => session.SendLocal(new MessageThatFails()))
                .DoNotFailOnErrorMessages())
            .WithEndpoint<EndpointThatHandlesErrorMessages>(b => b
                .DoNotFailOnErrorMessages())
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.MessageFailed, Is.True);
            Assert.That(context.TTBRHasExpiredAndMessageIsStillInErrorQueue, Is.True);
        }
    }

    class Context : ScenarioContext
    {
        public int ErrorQueueRetries;
        public bool MessageFailed { get; set; }
        public bool TTBRHasExpiredAndMessageIsStillInErrorQueue { get; set; }

        public void MaybeCompleted() => MarkAsCompleted(MessageFailed, TTBRHasExpiredAndMessageIsStillInErrorQueue);
    }

    class EndpointThatThrows : EndpointConfigurationBuilder
    {
        public EndpointThatThrows() =>
            EndpointSetup<DefaultServer>(c => c
                .SendFailedMessagesTo<EndpointThatHandlesErrorMessages>());

        class ThrowingMessageHandler(Context context) : IHandleMessages<MessageThatFails>
        {
            public Task Handle(MessageThatFails message, IMessageHandlerContext context1)
            {
                context.MessageFailed = true;
                context.MaybeCompleted();
                throw new SimulatedException();
            }
        }
    }

    class EndpointThatHandlesErrorMessages : EndpointConfigurationBuilder
    {
        public EndpointThatHandlesErrorMessages() => EndpointSetup<DefaultServer>(c => c.Recoverability().Immediate(s => s.NumberOfRetries(10)));

        class ErrorMessageHandler(Context testContext) : IHandleMessages<MessageThatFails>
        {
            public async Task Handle(MessageThatFails message, IMessageHandlerContext context)
            {
                if (testContext.ErrorQueueRetries > 0)
                {
                    testContext.TTBRHasExpiredAndMessageIsStillInErrorQueue = true;
                    testContext.MaybeCompleted();
                    return;
                }

                var ttbr = TimeSpan.Parse(context.MessageHeaders[Headers.TimeToBeReceived]);
                // wait longer than configured TTBR
                await Task.Delay(ttbr.Add(TimeSpan.FromSeconds(1)));

                // enforce message retry
                Interlocked.Increment(ref testContext.ErrorQueueRetries);
                throw new Exception("retry message");
            }
        }
    }

    [TimeToBeReceived("00:00:03")]
    public class MessageThatFails : IMessage;
}