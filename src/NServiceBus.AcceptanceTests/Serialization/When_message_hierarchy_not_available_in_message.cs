namespace NServiceBus.AcceptanceTests.Serialization;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using NServiceBus.Pipeline;
using NUnit.Framework;

public class When_message_hierarchy_not_available_in_message : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_handle_base_type_if_included_in_scanning()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Sender>(e => e.When(session => session.Send(new Message())))
            .WithEndpoint<ReceivingEndpoint>()
            .Run();

        Assert.That(context.MessageReceived, Is.True);
    }

    public class Context : ScenarioContext
    {
        public bool MessageReceived { get; set; }
    }

    // we need a different sender since the send will auto discover the BaseMessage type
    class Sender : EndpointConfigurationBuilder
    {
        public Sender() => EndpointSetup<DefaultServer>(c =>
        {
            c.Pipeline.Register(typeof(OverrideMessageTypeHeaderBehavior), "Sets the enclosed message type header to only the fullname");
            c.ConfigureRouting().RouteToEndpoint(typeof(Message), typeof(ReceivingEndpoint));
        });

        class OverrideMessageTypeHeaderBehavior : Behavior<IOutgoingPhysicalMessageContext>
        {
            public override Task Invoke(IOutgoingPhysicalMessageContext context, Func<Task> next)
            {
                context.Headers[Headers.EnclosedMessageTypes] = typeof(Message).FullName;
                return next();
            }
        }
    }

    public class ReceivingEndpoint : EndpointConfigurationBuilder
    {
        public ReceivingEndpoint() =>
            EndpointSetup<DefaultServer>()
                .IncludeType<Message>(); // this makes sure that both Message and BaseMessage is discovered

        [Handler]
        public class Handler(Context testContext) : IHandleMessages<BaseMessage>
        {
            public Task Handle(BaseMessage message, IMessageHandlerContext context)
            {
                testContext.MessageReceived = true;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class Message : BaseMessage;

    public class BaseMessage : IMessage;
}