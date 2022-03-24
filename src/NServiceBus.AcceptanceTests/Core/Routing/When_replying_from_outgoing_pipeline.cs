namespace NServiceBus.AcceptanceTests.Core.Routing
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    public class When_replying_from_outgoing_pipeline
    {
        [Test]
        public async Task Should_route_correctly()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<EndpointA>(e => e.When(s =>
                {
                    var sendOptions = new SendOptions();
                    sendOptions.RouteReplyTo(Conventions.EndpointNamingConvention(typeof(EndpointB)));
                    sendOptions.RouteToThisEndpoint();
                    return s.Send(new TriggerMessage(), sendOptions);
                }))
                .WithEndpoint<EndpointB>()
                .WithEndpoint<EndpointC>()
                .Done(c => c.ReplyReceived && c.BehaviorMessageReceived)
                .Run(TimeSpan.FromSeconds(15));
        }

        class Context : ScenarioContext
        {
            public bool ReplyReceived { get; set; }
            public bool BehaviorMessageReceived { get; set; }
        }

        class EndpointA : EndpointConfigurationBuilder
        {
            public EndpointA() => EndpointSetup<DefaultServer>(c =>
            {
                c.Pipeline.Register(new OutgoingPipelineBehaviorSendingMessages(), "test behavior");
                c.ConfigureTransport().Routing().RouteToEndpoint(typeof(BehaviorMessage), Conventions.EndpointNamingConvention(typeof(EndpointC)));
            });

            class TriggerMessageHandler : IHandleMessages<TriggerMessage>
            {
                public Task Handle(TriggerMessage message, IMessageHandlerContext context)
                {
                    var replyOptions = new ReplyOptions();
                    return context.Reply(new ReplyMessage(), replyOptions);
                }
            }

            class OutgoingPipelineBehaviorSendingMessages : Behavior<IOutgoingLogicalMessageContext>
            {
                public override async Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
                {
                    await next();
                    if (context.Message.MessageType == typeof(ReplyMessage))
                    {
                        await context.Send(new BehaviorMessage());
                    }
                }
            }
        }

        class EndpointB : EndpointConfigurationBuilder
        {
            public EndpointB() => EndpointSetup<DefaultServer>(c => c.ConfigureTransport().Routing().RouteToEndpoint(typeof(TriggerMessage), Conventions.EndpointNamingConvention(typeof(EndpointA))));

            class ReplyHandler : IHandleMessages<ReplyMessage>
            {
                Context testContext;

                public ReplyHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(ReplyMessage message, IMessageHandlerContext context)
                {
                    testContext.ReplyReceived = true;
                    return Task.FromResult(0);
                }
            }
        }

        class EndpointC : EndpointConfigurationBuilder
        {
            public EndpointC() => EndpointSetup<DefaultServer>();

            public class BehaviorMessageHandler : IHandleMessages<BehaviorMessage>
            {
                Context testContext;

                public BehaviorMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(BehaviorMessage message, IMessageHandlerContext context)
                {
                    testContext.BehaviorMessageReceived = true;
                    return Task.FromResult(0);
                }
            }
        }

        class TriggerMessage : IMessage
        {
        }

        class ReplyMessage : IMessage
        {
        }

        class BehaviorMessage : IMessage
        {
        }
    }
}