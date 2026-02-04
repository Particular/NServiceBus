namespace NServiceBus.AcceptanceTests.Serialization;

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
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.HandlerInvoked, Is.False);
            Assert.That(context.FailedMessages.Single().Value, Has.Count.EqualTo(1));
        }
        Exception exception = context.FailedMessages.Single().Value.Single().Exception;
        Assert.That(exception, Is.InstanceOf<MessageDeserializationException>());
    }

    public class Context : ScenarioContext
    {
        public bool HandlerInvoked { get; set; }
        public bool IncomingMessageReceived { get; set; }
    }

    public class ReceivingEndpoint : EndpointConfigurationBuilder
    {
        public ReceivingEndpoint() =>
            EndpointSetup<DefaultServer>(c =>
            {
                c.Pipeline.Register(typeof(TypeHeaderRemovingBehavior), "Removes the EnclosedMessageTypes header from incoming messages");
            });

        [Handler]
        public class MessageHandler(Context testContext) : IHandleMessages<MessageWithEmptyTypeHeader>
        {
            public Task Handle(MessageWithEmptyTypeHeader message, IMessageHandlerContext context)
            {
                testContext.HandlerInvoked = true;
                return Task.CompletedTask;
            }
        }

        class TypeHeaderRemovingBehavior(Context testContext) : Behavior<IIncomingPhysicalMessageContext>
        {
            public override Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
            {
                testContext.IncomingMessageReceived = true;
                // add some whitespace instead of removing the header completely
                context.Message.Headers[Headers.EnclosedMessageTypes] = "   ";
                testContext.MarkAsCompleted();
                return next();
            }
        }
    }

    public class MessageWithEmptyTypeHeader : IMessage;
}