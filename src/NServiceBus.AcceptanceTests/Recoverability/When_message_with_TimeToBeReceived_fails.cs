namespace NServiceBus.AcceptanceTests.Recoverability
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_message_with_TimeToBeReceived_fails : NServiceBusAcceptanceTest
    {
        // This test has repeatedly failed because the message took longer than the TTBR value to be received.
        // We assume this could be due to the parallel test execution.
        // If this test fails your build with this attribute set, please ping the NServiceBus maintainers.
        [NonParallelizable]
        [Test]
        public async Task Should_not_honor_TimeToBeReceived_for_error_message()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointThatThrows>(b => b
                    .When(session => session.SendLocal(new MessageThatFails()))
                    .DoNotFailOnErrorMessages())
                .WithEndpoint<EndpointThatHandlesErrorMessages>(b => b
                    .DoNotFailOnErrorMessages())
                .Done(c => c.MessageFailed && c.TTBRHasExpiredAndMessageIsStillInErrorQueue)
                .Run();

            Assert.IsTrue(context.MessageFailed);
            Assert.IsTrue(context.TTBRHasExpiredAndMessageIsStillInErrorQueue);
        }

        class Context : ScenarioContext
        {
            public int ErrorQueueRetries;
            public bool MessageFailed { get; set; }
            public bool TTBRHasExpiredAndMessageIsStillInErrorQueue { get; set; }
        }

        class EndpointThatThrows : EndpointConfigurationBuilder
        {
            public EndpointThatThrows()
            {
                EndpointSetup<DefaultServer>(c => c
                    .SendFailedMessagesTo<EndpointThatHandlesErrorMessages>());
            }

            class ThrowingMessageHandler : IHandleMessages<MessageThatFails>
            {
                public ThrowingMessageHandler(Context context)
                {
                    this.context = context;
                }

                public Task Handle(MessageThatFails message, IMessageHandlerContext context1)
                {
                    context.MessageFailed = true;
                    throw new SimulatedException();
                }

                Context context;
            }
        }

        class EndpointThatHandlesErrorMessages : EndpointConfigurationBuilder
        {
            public EndpointThatHandlesErrorMessages()
            {
                EndpointSetup<DefaultServer>(c => c.Recoverability().Immediate(s => s.NumberOfRetries(10)));
            }

            class ErrorMessageHandler : IHandleMessages<MessageThatFails>
            {
                public ErrorMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public async Task Handle(MessageThatFails message, IMessageHandlerContext context)
                {
                    if (testContext.ErrorQueueRetries > 0)
                    {
                        testContext.TTBRHasExpiredAndMessageIsStillInErrorQueue = true;
                        return;
                    }

                    var ttbr = TimeSpan.Parse(context.MessageHeaders[Headers.TimeToBeReceived]);
                    // wait longer than configured TTBR
                    await Task.Delay(ttbr.Add(TimeSpan.FromSeconds(1)));

                    // enforce message retry
                    Interlocked.Increment(ref testContext.ErrorQueueRetries);
                    throw new Exception("retry message");
                }

                Context testContext;
            }
        }

        [TimeToBeReceived("00:00:03")]
        public class MessageThatFails : IMessage
        {
        }
    }
}