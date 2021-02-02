﻿namespace NServiceBus.AcceptanceTests.Routing
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_replying_to_message_with_interface_and_unobtrusive : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_reply_to_originator()
        {
            var ctx = await Scenario.Define<Context>()
                .WithEndpoint<SendingEndpoint>(c => c
                    .When(b => b.Send(new MyMessage())))
                .WithEndpoint<ReplyingEndpoint>()
                .WithEndpoint<OtherEndpoint>()
                .Done(c => c.SendingEndpointGotResponse)
                .Run();

            Assert.IsTrue(ctx.SendingEndpointGotResponse);
            Assert.IsFalse(ctx.OtherEndpointGotResponse);
        }

        public class Context : ScenarioContext
        {
            public bool SendingEndpointGotResponse { get; set; }
            public bool OtherEndpointGotResponse { get; set; }
        }

        public class SendingEndpoint : EndpointConfigurationBuilder
        {
            public SendingEndpoint()
            {
                EndpointSetup<DefaultPublisher>(c =>
                {
                    c.Conventions().DefiningMessagesAs(t => t.Namespace != null && t.Name.StartsWith("My"));
                    c.ConfigureTransport().Routing().RouteToEndpoint(typeof(MyMessage), typeof(ReplyingEndpoint));
                });
            }

            public class ResponseHandler : IHandleMessages<MyReply>
            {
                public ResponseHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(MyReply messageThatIsEnlisted, IMessageHandlerContext context)
                {
                    testContext.SendingEndpointGotResponse = true;
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class OtherEndpoint : EndpointConfigurationBuilder
        {
            public OtherEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.Conventions().DefiningMessagesAs(t => t.Namespace != null && t.Name.StartsWith("My")));
            }

            public class ResponseHandler : IHandleMessages<MyReply>
            {
                public ResponseHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(MyReply messageThatIsEnlisted, IMessageHandlerContext context)
                {
                    testContext.OtherEndpointGotResponse = true;
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class ReplyingEndpoint : EndpointConfigurationBuilder
        {
            public ReplyingEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.Conventions().DefiningMessagesAs(t => t.Namespace != null && t.Name.StartsWith("My")))
                    .ExcludeType<MyReply>(); // remove that type from assembly scanning to simulate what would happen with true unobtrusive mode
            }

            public class MessageHandler : IHandleMessages<MyMessage>
            {
                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    return context.Reply<MyReply>(m => { });
                }
            }
        }

        public class MyMessage
        {
        }

#pragma warning disable IDE1006 // Naming Styles
        public interface MyReply
#pragma warning restore IDE1006 // Naming Styles
        {
        }
    }
}