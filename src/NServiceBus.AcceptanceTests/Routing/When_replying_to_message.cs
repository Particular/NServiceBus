namespace NServiceBus.AcceptanceTests.Routing
{
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_replying_to_message : NServiceBusAcceptanceTest
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

        [Test]
        public async Task Should_reply_to_configured_return_address()
        {
            const string returnAddress = "ReplyingToMessage.OtherEndpoint";

            var ctx = await Scenario.Define<Context>()
                .WithEndpoint<SendingEndpoint>(c => c
                    .CustomConfig(b => b.OverridePublicReturnAddress(returnAddress))
                    .When(b => b.Send(new MyMessage())))
                .WithEndpoint<ReplyingEndpoint>()
                .WithEndpoint<OtherEndpoint>()
                .Done(c => c.OtherEndpointGotResponse)
                .Run();

            Assert.IsTrue(ctx.OtherEndpointGotResponse);
            Assert.IsFalse(ctx.SendingEndpointGotResponse);
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
                EndpointSetup<DefaultPublisher>().AddMapping<MyMessage>(typeof(ReplyingEndpoint));
            }

            public class ResponseHandler : IHandleMessages<MyReply>
            {
                public Context Context { get; set; }

                public Task Handle(MyReply messageThatIsEnlisted, IMessageHandlerContext context)
                {
                    Context.SendingEndpointGotResponse = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class OtherEndpoint : EndpointConfigurationBuilder
        {
            public OtherEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class ResponseHandler : IHandleMessages<MyReply>
            {
                public Context Context { get; set; }

                public Task Handle(MyReply messageThatIsEnlisted, IMessageHandlerContext context)
                {
                    Context.OtherEndpointGotResponse = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class ReplyingEndpoint : EndpointConfigurationBuilder
        {
            public ReplyingEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MessageHandler : IHandleMessages<MyMessage>
            {
                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    return context.Reply(new MyReply());
                }
            }
        }

        public class MyMessage : IMessage
        {
        }
        
        public class MyReply : IMessage
        {
        }
    }
}
