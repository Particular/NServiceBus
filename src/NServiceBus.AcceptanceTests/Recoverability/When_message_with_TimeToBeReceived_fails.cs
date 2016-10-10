﻿namespace NServiceBus.AcceptanceTests.Recoverability
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
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
                .WithEndpoint<EndpointThatHandlesErrorMessages>()
                .Done(c => c.MessageFailed && c.TTBRHasExpiredAndMessageIsStillInErrorQueue)
                .Run();

            Assert.IsTrue(context.MessageFailed);
            Assert.IsTrue(context.TTBRHasExpiredAndMessageIsStillInErrorQueue);
        }

        class Context : ScenarioContext
        {
            public bool MessageFailed { get; set; }
            public DateTime? FirstTimeProcessedByErrorHandler { get; set; }
            public bool TTBRHasExpiredAndMessageIsStillInErrorQueue { get; set; }
        }

        class EndpointThatThrows : EndpointConfigurationBuilder
        {
            public EndpointThatThrows()
            {
                EndpointSetup<DefaultServer>(b => { b.SendFailedMessagesTo("errorQueueForAcceptanceTest"); });
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
                EndpointSetup<DefaultServer>()
                    .CustomEndpointName("errorQueueForAcceptanceTest");
            }

            class ErrorMessageHandler : IHandleMessages<MessageThatFails>
            {
                public ErrorMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(MessageThatFails message, IMessageHandlerContext context)
                {
                    var errorProcessingStarted = DateTime.Now;
                    if (testContext.FirstTimeProcessedByErrorHandler == null)
                    {
                        testContext.FirstTimeProcessedByErrorHandler = errorProcessingStarted;
                    }

                    var ttbr = TimeSpan.Parse(context.MessageHeaders[Headers.TimeToBeReceived]);
                    var ttbrExpired = errorProcessingStarted > testContext.FirstTimeProcessedByErrorHandler.Value + ttbr;
                    if (ttbrExpired)
                    {
                        testContext.TTBRHasExpiredAndMessageIsStillInErrorQueue = true;
                        var timeElapsedSinceFirstHandlingOfErrorMessage = errorProcessingStarted - testContext.FirstTimeProcessedByErrorHandler.Value;
                        Console.WriteLine("Error message not removed because of TTBR({0}) after {1}. Succeeded.", ttbr, timeElapsedSinceFirstHandlingOfErrorMessage);
                    }
                    else
                    {
                        return context.HandleCurrentMessageLater();
                    }

                    return Task.FromResult(0); // ignore messages from previous test runs
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