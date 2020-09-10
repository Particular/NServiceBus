namespace NServiceBus.AcceptanceTests.Routing
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NUnit.Framework;

    [TestFixture]
    public class When_routing_reply_to_specific_address : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_route_reply_to_instance_specific_queue()
        {
            var replyHandlerAddress = Conventions.EndpointNamingConvention(typeof(ReplyHandler));

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Sender>(e => e
                    .When(s =>
                    {
                        var options = new SendOptions();
                        options.RouteReplyTo(replyHandlerAddress);
                        return s.Send(new RequestReplyMessage(), options);
                    }))
                .WithEndpoint<Replier>()
                .WithEndpoint<ReplyHandler>()
                .Done(c => c.ReplyReceived)
                .Run();

            Assert.IsTrue(context.ReplyReceived);
            StringAssert.Contains(replyHandlerAddress, context.ReplyToAddress);
        }

        class Context : ScenarioContext
        {
            public string ReplyToAddress { get; set; }
            public bool ReplyReceived { get; set; }
        }

        class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.ConfigureTransport().Routing().RouteToEndpoint(typeof(RequestReplyMessage), typeof(Replier));
                });
            }
        }

        class Replier : EndpointConfigurationBuilder
        {
            public Replier()
            {
                EndpointSetup<DefaultServer>();
            }

            class RequestReplyMessageHandler : IHandleMessages<RequestReplyMessage>
            {
                public RequestReplyMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(RequestReplyMessage message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    testContext.ReplyToAddress = context.ReplyToAddress;
                    return context.Reply(new ReplyMessage());
                }

                Context testContext;
            }
        }

        class ReplyHandler : EndpointConfigurationBuilder
        {
            public ReplyHandler()
            {
                EndpointSetup<DefaultServer>();
            }

            class ReplyMessageHandler : IHandleMessages<ReplyMessage>
            {
                public ReplyMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(ReplyMessage message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    testContext.ReplyReceived = true;
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class RequestReplyMessage : ICommand
        {
        }

        public class ReplyMessage : IMessage
        {
        }
    }
}