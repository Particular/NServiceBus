namespace NServiceBus.AcceptanceTests.Core.Routing;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Customization;
using EndpointTemplates;
using NServiceBus.Pipeline;
using NUnit.Framework;

public class When_inner_send_with_outer_immediate_dispatch : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_not_apply_immediate_dispatch_to_inner_send()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointA>(c => c
                .DoNotFailOnErrorMessages()
                .When(s => s.SendLocal(new TriggerMessage())))
            .WithEndpoint<EndpointB>()
            .WithEndpoint<EndpointC>()
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.MessageBReceived, Is.True);
            Assert.That(context.MessageCReceived, Is.False);
        }
    }

    public class Context : ScenarioContext
    {
        public bool MessageBReceived { get; set; }
        public bool MessageCReceived { get; set; }
    }

    public class EndpointA : EndpointConfigurationBuilder
    {
        public EndpointA() =>
            EndpointSetup<DefaultServer>(c =>
            {
                c.ConfigureRouting().RouteToEndpoint(typeof(MessageToEndpointB), typeof(EndpointB));
                c.ConfigureRouting().RouteToEndpoint(typeof(MessageToEndpointC), typeof(EndpointC));
                c.Pipeline.Register(new OutgoingBehaviorWithSend(), "sends a message as part of an incoming message pipeline");
            });

        [Handler]
        public class TriggerMessageHandler : IHandleMessages<TriggerMessage>
        {
            public Task Handle(TriggerMessage message, IMessageHandlerContext context)
            {
                // "outer send"
                var sendOptions = new SendOptions();
                sendOptions.RequireImmediateDispatch();
                return context.Send(new MessageToEndpointB(), sendOptions);
            }
        }

        class OutgoingBehaviorWithSend : Behavior<IOutgoingLogicalMessageContext>
        {
            public override async Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
            {
                await next();
                if (context.Message.MessageType == typeof(MessageToEndpointB))
                {
                    // "inner send"
                    await context.Send(new MessageToEndpointC()); // no immediate dispatch
                    throw new Exception(); // batching should prevent message C from being dispatched.
                }
            }
        }
    }

    public class EndpointB : EndpointConfigurationBuilder
    {
        public EndpointB() => EndpointSetup<DefaultServer>();

        [Handler]
        public class EndpointBHandler(Context testContext) : IHandleMessages<MessageToEndpointB>
        {
            public Task Handle(MessageToEndpointB message, IMessageHandlerContext context)
            {
                testContext.MessageBReceived = true;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class EndpointC : EndpointConfigurationBuilder
    {
        public EndpointC() => EndpointSetup<DefaultServer>();

        [Handler]
        public class EndpointCHandler(Context testContext) : IHandleMessages<MessageToEndpointC>
        {
            public Task Handle(MessageToEndpointC message, IMessageHandlerContext context)
            {
                testContext.MessageCReceived = true;
                return Task.CompletedTask;
            }
        }
    }

    public class MessageToEndpointB : IMessage;

    public class MessageToEndpointC : IMessage;

    public class TriggerMessage : IMessage;
}
