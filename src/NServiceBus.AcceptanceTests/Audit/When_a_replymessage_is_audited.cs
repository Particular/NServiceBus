namespace NServiceBus.AcceptanceTests.Audit
{
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using MessageMutator;
    using NUnit.Framework;
    using AcceptanceTesting.Customization;

    public class When_a_replymessage_is_audited : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_audit_the_message()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Server>()
                .WithEndpoint<EndpointWithAuditOn>(b => b.When(session => session.Send(new Request())))
                .WithEndpoint<AuditSpyEndpoint>()
                .Done(c => c.MessageAudited)
                .Run();

            Assert.True(context.MessageProcessed);
            Assert.True(context.MessageAudited);
            Assert.AreEqual("SomeValue", context.HeaderValue);
        }

        public static byte Checksum(byte[] data)
        {
            var longSum = data.Sum(x => (long)x);
            return unchecked((byte)longSum);
        }

        public class Context : ScenarioContext
        {
            public bool MessageAudited { get; set; }
            public bool MessageProcessed { get; set; }
            public string HeaderValue { get; set; }
        }

        public class Server : EndpointFromTemplate<DefaultServer>
        {
            class RequestHandler : IHandleMessages<Request>
            {
                public Task Handle(Request message, IMessageHandlerContext context)
                {
                    var replyOptions = new ReplyOptions();

                    replyOptions.SetHeader("MyHeader", "SomeValue");

                    return context.Reply(new ResponseToBeAudited(), replyOptions);
                }
            }
        }

        public class EndpointWithAuditOn : EndpointConfigurationBuilder
        {
            public EndpointWithAuditOn()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.DisableFeature<Outbox>();
                    c.AuditProcessedMessagesTo<AuditSpyEndpoint>();
                    c.ConfigureRouting().RouteToEndpoint(typeof(Request), typeof(Server));
                });
            }

            public class MessageToBeAuditedHandler : IHandleMessages<ResponseToBeAudited>
            {
                public MessageToBeAuditedHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(ResponseToBeAudited message, IMessageHandlerContext context)
                {
                    testContext.HeaderValue = context.MessageHeaders["MyHeader"];
                    testContext.MessageProcessed = true;
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        class AuditSpyEndpoint : EndpointConfigurationBuilder
        {
            public AuditSpyEndpoint()
            {
                EndpointSetup<DefaultServer, Context>((config, context) => config.RegisterMessageMutator(new BodySpy(context)));
            }

            class BodySpy : IMutateIncomingTransportMessages
            {
                public BodySpy(Context testContext)
                {
                    this.testContext = testContext;
                }
                public Task MutateIncoming(MutateIncomingTransportMessageContext context)
                {
                    testContext.MessageAudited = true;
                    return Task.FromResult(0);
                }

                Context testContext;
            }

            public class MessageToBeAuditedHandler : IHandleMessages<ResponseToBeAudited>
            {
                public Task Handle(ResponseToBeAudited message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }
            }
        }

        public class ResponseToBeAudited : IMessage
        {
        }

        public class Request : IMessage
        {
        }
    }
}