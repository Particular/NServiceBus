namespace NServiceBus.AcceptanceTests.Recoverability
{
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_unrecoverable_exception_thrown : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_move_to_error_queue_without_retries()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointThatThrowsUnrecoverableExceptions>(b => b
                    .DoNotFailOnErrorMessages()
                    .When((session, c) => session.SendLocal(new MessageOne()))
                )
                .Done(c => c.FailedMessages.Any())
                .Run();

            var failedMessages = context.FailedMessages.Single().Value;
            Assert.AreEqual(1, failedMessages.Count);
            Assert.IsInstanceOf<UnrecoverableException>(failedMessages.First().Exception);
            Assert.AreEqual(1, context.NrOfTimesHandlerWasInvoked);
        }

        [Test]
        public async Task Should_move_to_error_queue_without_retries_when_inheriting_from_unrecoverable_ex()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointThatThrowsUnrecoverableExceptions>(b => b
                    .DoNotFailOnErrorMessages()
                    .When((session, c) => session.SendLocal(new MessageTwo()))
                )
                .Done(c => c.FailedMessages.Any())
                .Run();

            var failedMessages = context.FailedMessages.Single().Value;
            Assert.AreEqual(1, failedMessages.Count);
            Assert.IsInstanceOf<SpecificException>(failedMessages.First().Exception);
            Assert.AreEqual(1, context.NrOfTimesHandlerWasInvoked);
        }

        class Context : ScenarioContext
        {
            public bool MessageMovedToErrorQueue { get; set; }
            public int NrOfTimesHandlerWasInvoked { get; set; }
        }

        class EndpointThatThrowsUnrecoverableExceptions : EndpointConfigurationBuilder
        {
            public EndpointThatThrowsUnrecoverableExceptions()
            {
                EndpointSetup<DefaultServer>();
            }

            class MessageOneHandler : IHandleMessages<MessageOne>
            {
                public MessageOneHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(MessageOne message, IMessageHandlerContext context)
                {
                    testContext.NrOfTimesHandlerWasInvoked++;
                    throw new UnrecoverableException("This exception is unrecoverable");
                }

                Context testContext;
            }

            class MessageTwoHandler : IHandleMessages<MessageTwo>
            {
                public MessageTwoHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(MessageTwo initiatingMessage, IMessageHandlerContext context)
                {
                    testContext.NrOfTimesHandlerWasInvoked++;
                    throw new SpecificException("This exception is unrecoverable");
                }

                Context testContext;
            }
        }

        class SpecificException : UnrecoverableException
        {
            public SpecificException(string message) : base(message)
            {
            }
        }

        public class MessageOne : IMessage
        {
        }

        public class MessageTwo : IMessage
        {
        }
    }
}