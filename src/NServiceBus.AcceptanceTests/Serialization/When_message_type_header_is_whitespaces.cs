namespace NServiceBus.AcceptanceTests.Serialization
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    public class When_message_type_header_is_whitespaces : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_move_message_to_error_queue()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<ReceivingEndpoint>(e => e
                    .DoNotFailOnErrorMessages()
                    .When(s => s.SendLocal(new MessageWithEmptyTypeHeader())))
                .Done(c => c.IncomingMessageReceived)
                .Run(TimeSpan.FromSeconds(20));

            Assert.IsFalse(context.HandlerInvoked);
            Assert.AreEqual(1, context.FailedMessages.Single().Value.Count);
            Exception exception = context.FailedMessages.Single().Value.Single().Exception;
            Assert.IsInstanceOf<MessageDeserializationException>(exception);
        }

        class Context : ScenarioContext
        {
            public bool HandlerInvoked { get; set; }
            public bool IncomingMessageReceived { get; set; }
        }

        class ReceivingEndpoint : EndpointConfigurationBuilder
        {
            public ReceivingEndpoint() =>
                EndpointSetup<DefaultServer>(c =>
                {
                    c.Pipeline.Register(typeof(TypeHeaderRemovingBehavior), "Removes the EnclosedMessageTypes header from incoming messages");
                });

            public class MessageHandler : IHandleMessages<MessageWithEmptyTypeHeader>
            {
                Context testContext;

                public MessageHandler(Context testContext) => this.testContext = testContext;

                public Task Handle(MessageWithEmptyTypeHeader message, IMessageHandlerContext context)
                {
                    testContext.HandlerInvoked = true;
                    return Task.CompletedTask;
                }
            }

            class TypeHeaderRemovingBehavior : Behavior<IIncomingPhysicalMessageContext>
            {
                Context testContext;

                public TypeHeaderRemovingBehavior(Context testContext) => this.testContext = testContext;

                public override Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
                {
                    testContext.IncomingMessageReceived = true;

                    context.Message.Headers[Headers.EnclosedMessageTypes] = "   ";

                    return next();
                }
            }
        }

        public class MessageWithEmptyTypeHeader : IMessage
        {
        }
    }
}