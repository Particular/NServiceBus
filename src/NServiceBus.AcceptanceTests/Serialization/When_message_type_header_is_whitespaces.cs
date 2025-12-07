namespace NServiceBus.AcceptanceTests.Serialization;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NServiceBus.Pipeline;
using NUnit.Framework;

public class When_message_type_header_is_whitespaces : NServiceBusAcceptanceTest
{
    [Test, CancelAfter(20_000)]
    public async Task Should_move_message_to_error_queue(CancellationToken cancellationToken = default)
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<ReceivingEndpoint>(e => e
                .DoNotFailOnErrorMessages()
                .When(s => s.SendLocal(new MessageWithEmptyTypeHeader())))
            .Done(c => c.IncomingMessageReceived)
            .Run(cancellationToken);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.HandlerInvoked, Is.False);
            Assert.That(context.FailedMessages.Single().Value, Has.Count.EqualTo(1));
        }
        Exception exception = context.FailedMessages.Single().Value.Single().Exception;
        Assert.That(exception, Is.InstanceOf<MessageDeserializationException>());
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

                // add some whitespace instead of removing the header completely
                context.Message.Headers[Headers.EnclosedMessageTypes] = "   ";

                return next();
            }
        }
    }

    public class MessageWithEmptyTypeHeader : IMessage
    {
    }
}