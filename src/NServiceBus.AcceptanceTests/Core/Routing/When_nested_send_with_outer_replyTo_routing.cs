namespace NServiceBus.AcceptanceTests.Core.Routing
{
    using System;
    using System.Reflection;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    public class When_nested_send_with_outer_replyTo_routing : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_apply_default_reply_in_inner_send()
        {
            const string customReplyAddress = "Tatooine";

            var context = await Scenario.Define<Context>()
                .WithEndpoint<SenderEndpoint>(c => c
                    .When(s =>
                    {
                        var sendOptions = new SendOptions();
                        sendOptions.RouteReplyTo(customReplyAddress);
                        return s.Send(new OuterMessage(), sendOptions);
                    }))
                .WithEndpoint<ReplyEndpoint>()
                .Done(c => c.OuterMessageReceived && c.InnerMessageReceived)
                .Run();

            Assert.AreEqual(customReplyAddress, context.OuterMessageReplyAddress);
            Assert.AreEqual(Conventions.EndpointNamingConvention(typeof(SenderEndpoint)), context.InnerMessageReplyAddress);
        }

        class Context : ScenarioContext
        {
            public bool OuterMessageReceived { get; set; }
            public bool InnerMessageReceived { get; set; }
            public string OuterMessageReplyAddress { get; set; }
            public string InnerMessageReplyAddress { get; set; }
        }

        class SenderEndpoint : EndpointConfigurationBuilder
        {
            public SenderEndpoint() => EndpointSetup<DefaultServer>(c =>
            {
                c.ConfigureRouting().RouteToEndpoint(Assembly.GetExecutingAssembly(), Conventions.EndpointNamingConvention(typeof(ReplyEndpoint)));
                c.Pipeline.Register(new InnerSendBehavior(), "sends an inner message on send operations");
            });

            class InnerSendBehavior : Behavior<IOutgoingLogicalMessageContext>
            {
                public override async Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
                {
                    await next();

                    if (context.Message.MessageType == typeof(OuterMessage))
                    {
                        await context.Send(new InnerMessage());
                    }
                }
            }
        }

        class ReplyEndpoint : EndpointConfigurationBuilder
        {
            public ReplyEndpoint() => EndpointSetup<DefaultServer>();

            class MessageHandler : IHandleMessages<OuterMessage>, IHandleMessages<InnerMessage>
            {
                Context testContext;

                public MessageHandler(Context testContext) => this.testContext = testContext;

                public Task Handle(OuterMessage message, IMessageHandlerContext context)
                {
                    testContext.OuterMessageReceived = true;
                    testContext.OuterMessageReplyAddress = context.ReplyToAddress;
                    return Task.FromResult(0);
                }

                public Task Handle(InnerMessage message, IMessageHandlerContext context)
                {
                    testContext.InnerMessageReceived = true;
                    testContext.InnerMessageReplyAddress = context.ReplyToAddress;
                    return Task.FromResult(0);
                }
            }
        }

        public class OuterMessage : IMessage
        {
        }

        public class InnerMessage : IMessage
        {
        }
    }
}