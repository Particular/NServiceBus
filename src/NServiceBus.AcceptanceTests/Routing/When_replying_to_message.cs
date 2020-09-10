namespace NServiceBus.AcceptanceTests.Routing
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NUnit.Framework;
    using Conventions = AcceptanceTesting.Customization.Conventions;

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
        public async Task Should_reply_to_shared_queue_by_default()
        {
            const string instanceDiscriminator = "instance-55";

            var ctx = await Scenario.Define<Context>()
                .WithEndpoint<SendingEndpoint>(c => c
                    .CustomConfig(cfg => cfg.MakeInstanceUniquelyAddressable(instanceDiscriminator))
                    .When(b => b.Send(new MyMessage())))
                .WithEndpoint<ReplyingEndpoint>()
                .Done(c => c.SendingEndpointGotResponse)
                .Run();

            Assert.IsTrue(ctx.SendingEndpointGotResponse);
            StringAssert.DoesNotContain(instanceDiscriminator, ctx.ReplyToAddress);
        }

        [Test]
        public async Task Should_reply_to_configured_return_address()
        {
            var ctx = await Scenario.Define<Context>()
                .WithEndpoint<SendingEndpoint>(c => c
                    .CustomConfig(b => b.OverridePublicReturnAddress(Conventions.EndpointNamingConvention(typeof(OtherEndpoint))))
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
            public string ReplyToAddress { get; set; }
        }

        public class SendingEndpoint : EndpointConfigurationBuilder
        {
            public SendingEndpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.ConfigureTransport().Routing().RouteToEndpoint(typeof(MyMessage), typeof(ReplyingEndpoint));
                });
            }

            public class ResponseHandler : IHandleMessages<MyReply>
            {
                public ResponseHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(MyReply messageThatIsEnlisted, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
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
                EndpointSetup<DefaultServer>();
            }

            public class ResponseHandler : IHandleMessages<MyReply>
            {
                public ResponseHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(MyReply messageThatIsEnlisted, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
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
                EndpointSetup<DefaultServer>();
            }

            public class MessageHandler : IHandleMessages<MyMessage>
            {
                public MessageHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(MyMessage message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    testContext.ReplyToAddress = context.ReplyToAddress;
                    return context.Reply(new MyReply());
                }

                Context testContext;
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