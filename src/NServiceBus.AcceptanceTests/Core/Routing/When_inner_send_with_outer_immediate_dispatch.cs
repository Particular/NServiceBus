﻿namespace NServiceBus.AcceptanceTests.Core.Routing
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
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
                .Done(c => c.MessageBReceived)
                .Run(TimeSpan.FromSeconds(15));

            Assert.IsTrue(context.MessageBReceived);
            Assert.IsFalse(context.MessageCReceived);
        }

        class Context : ScenarioContext
        {
            public bool MessageBReceived { get; set; }
            public bool MessageCReceived { get; set; }
        }

        class EndpointA : EndpointConfigurationBuilder
        {
            public EndpointA()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.ConfigureTransport().Routing().RouteToEndpoint(typeof(MessageToEndpointB), typeof(EndpointB));
                    c.ConfigureTransport().Routing().RouteToEndpoint(typeof(MessageToEndpointC), typeof(EndpointC));
                    c.Pipeline.Register(new OutgoingBehaviorWithSend(), "sends a message as part of an incoming message pipeline");
                });
            }

            class TriggerMessageHandler : IHandleMessages<TriggerMessage>
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

        class EndpointB : EndpointConfigurationBuilder
        {
            public EndpointB() => EndpointSetup<DefaultServer>();

            public class EndpointBHandler : IHandleMessages<MessageToEndpointB>
            {
                Context testContext;

                public EndpointBHandler(Context testContext) => this.testContext = testContext;

                public Task Handle(MessageToEndpointB message, IMessageHandlerContext context)
                {
                    testContext.MessageBReceived = true;
                    return Task.FromResult(0);
                }
            }
        }

        class EndpointC : EndpointConfigurationBuilder
        {
            public EndpointC() => EndpointSetup<DefaultServer>();

            public class EndpointCHandler : IHandleMessages<MessageToEndpointC>
            {
                Context testContext;

                public EndpointCHandler(Context testContext) => this.testContext = testContext;

                public Task Handle(MessageToEndpointC message, IMessageHandlerContext context)
                {
                    testContext.MessageCReceived = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class MessageToEndpointB : IMessage
        {
        }

        public class MessageToEndpointC : IMessage
        {
        }

        public class TriggerMessage : IMessage
        {
        }
    }
}
