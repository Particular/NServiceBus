namespace NServiceBus.AcceptanceTests.Recoverability
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NUnit.Framework;

    public class When_message_with_TimeToBeReceived_fails : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_honor_TimeToBeReceived_for_error_message()
        {
            var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointThatThrows>(b => b.Given(bus => bus.SendLocalAsync(new MessageThatFails())))
            .WithEndpoint<EndpointThatHandlesErrorMessages>()
            .AllowSimulatedExceptions()
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
                EndpointSetup<DefaultServer>(b =>
                {
                    b.DisableFeature<Features.SecondLevelRetries>();
                    b.SendFailedMessagesTo("errorqueueforacceptancetest");
                })
                    .WithConfig<TransportConfig>(c =>
                    {
                        c.MaxRetries = 0;
                    });
            }

            class ThrowingMessageHandler : IHandleMessages<MessageThatFails>
            {
                Context context;

                public ThrowingMessageHandler(Context context)
                {
                    this.context = context;
                }

                public Task Handle(MessageThatFails message)
                {
                    context.MessageFailed = true;
                    throw new SimulatedException();
                }
            }
        }

        class EndpointThatHandlesErrorMessages : EndpointConfigurationBuilder
        {

            public EndpointThatHandlesErrorMessages()
            {
                EndpointSetup<DefaultServer>()
                    .CustomEndpointName("errorqueueforacceptancetest");
            }

            class ErrorMessageHandler : IHandleMessages<MessageThatFails>
            {
                Context context;
                IBus bus;

                public ErrorMessageHandler(Context context, IBus bus)
                {
                    this.context = context;
                    this.bus = bus;
                }

                public Task Handle(MessageThatFails message)
                {
                    var errorProcessingStarted = DateTime.Now;
                    if (context.FirstTimeProcessedByErrorHandler == null)
                    {
                        context.FirstTimeProcessedByErrorHandler = errorProcessingStarted;
                    }

                    var ttbr = TimeSpan.Parse(bus.CurrentMessageContext.Headers[Headers.TimeToBeReceived]);
                    var ttbrExpired = errorProcessingStarted > (context.FirstTimeProcessedByErrorHandler.Value + ttbr);
                    if (ttbrExpired)
                    {
                        context.TTBRHasExpiredAndMessageIsStillInErrorQueue = true;
                        var timeElapsedSinceFirstHandlingOfErrorMessage = errorProcessingStarted - context.FirstTimeProcessedByErrorHandler.Value;
                        Console.WriteLine("Error message not removed because of TTBR({0}) after {1}. Success.", ttbr, timeElapsedSinceFirstHandlingOfErrorMessage);
                    }
                    else
                    {
                        return bus.HandleCurrentMessageLaterAsync();
                    }

                    return Task.FromResult(0); // ignore messages from previous test runs
                }
            }
        }

        [Serializable]
        [TimeToBeReceived("00:00:03")]
        class MessageThatFails : IMessage
        {
        }
    }
}
