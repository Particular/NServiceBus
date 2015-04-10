namespace NServiceBus.AcceptanceTests.Audit
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NUnit.Framework;

    public class When_message_with_TimeToBeReceived_fails : NServiceBusAcceptanceTest
    {

        [Test]
        public void Should_not_honor_TimeToBeReceived_for_error_message()
        {
            var context = new Context();
            Scenario.Define(context)
            .WithEndpoint<EndpointThatThrows>(b => b.Given(Send()))
            .WithEndpoint<EndpointThatHandlesErrorMessages>()
            .AllowExceptions()
            .Done(c => c.MessageFailed && c.TTBRHasExpiredAndMessageIsStillInErrorQueue)
            .Run();
            Assert.IsTrue(context.MessageFailed);
            Assert.IsTrue(context.TTBRHasExpiredAndMessageIsStillInErrorQueue);
        }

        static Action<IBus> Send()
        {
            return bus => bus.SendLocal(new MessageThatFails());
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
                EndpointSetup<DefaultServer>(b => b.DisableFeature<Features.SecondLevelRetries>())
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

                public void Handle(MessageThatFails message)
                {
                    context.MessageFailed = true;
                    throw new Exception("Simulated exception");
                }
            }
        }

        class EndpointThatHandlesErrorMessages : EndpointConfigurationBuilder
        {

            public EndpointThatHandlesErrorMessages()
            {
                EndpointSetup<DefaultServer>()
                    .CustomEndpointName("error");
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

                public void Handle(MessageThatFails message)
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
                        bus.HandleCurrentMessageLater();
                    }
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
