namespace NServiceBus.AcceptanceTests.Core.Routing
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    public class When_sending_from_incoming_pipeline : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_use_default_routing_when_empty_send_options()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointA>(e => e
                    .When(s => s.SendLocal(new TriggerMessage())))
                .WithEndpoint<EndpointB>()
                .WithEndpoint<EndpointC>()
                .Done(c => c.MessageBReceived && c.MessageCReceived)
                .Run(TimeSpan.FromSeconds(15));

            Assert.IsTrue(context.MessageBReceived);
            Assert.IsTrue(context.MessageCReceived);
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
                    c.Pipeline.Register(new IncomingBehaviorWithSendLocal(), "sends a message as part of an incoming message pipeline");
                });
            }

            class IncomingBehaviorWithSendLocal : Behavior<IIncomingLogicalMessageContext>
            {
                public override async Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
                {
                    await context.Send(new MessageToEndpointC());
                    await next();
                }
            }

            class TriggerMessageHandler : IHandleMessages<TriggerMessage>
            {
                public Task Handle(TriggerMessage message, IMessageHandlerContext context)
                {
                    return context.Send(new MessageToEndpointB()); // empty sendoptions
                }
            }
        }

        class EndpointB : EndpointConfigurationBuilder
        {
            public EndpointB()
            {
                EndpointSetup<DefaultServer>();
            }

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
            public EndpointC()
            {
                EndpointSetup<DefaultServer>();
            }

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
    }

    class MessageToEndpointB : IMessage
    {
    }

    class MessageToEndpointC : IMessage
    {
    }

    class TriggerMessage : IMessage
    {
    }
}