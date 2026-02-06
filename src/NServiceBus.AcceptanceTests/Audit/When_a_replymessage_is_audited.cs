namespace NServiceBus.AcceptanceTests.Audit;

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
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.MessageProcessed, Is.True);
            Assert.That(context.MessageAudited, Is.True);
            Assert.That(context.HeaderValue, Is.EqualTo("SomeValue"));
        }
    }

    public class Context : ScenarioContext
    {
        public bool MessageAudited { get; set; }
        public bool MessageProcessed { get; set; }
        public string HeaderValue { get; set; }
    }

    public class Server : EndpointConfigurationBuilder
    {
        public Server() => EndpointSetup<DefaultServer>();

        [Handler]
        public class RequestHandler : IHandleMessages<Request>
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
        public EndpointWithAuditOn() =>
            EndpointSetup<DefaultServer>(c =>
            {
                c.DisableFeature<Outbox>();
                c.AuditProcessedMessagesTo<AuditSpyEndpoint>();
                c.ConfigureRouting().RouteToEndpoint(typeof(Request), typeof(Server));
            });

        [Handler]
        public class MessageToBeAuditedHandler(Context testContext) : IHandleMessages<ResponseToBeAudited>
        {
            public Task Handle(ResponseToBeAudited message, IMessageHandlerContext context)
            {
                testContext.HeaderValue = context.MessageHeaders["MyHeader"];
                testContext.MessageProcessed = true;
                return Task.CompletedTask;
            }
        }
    }

    public class AuditSpyEndpoint : EndpointConfigurationBuilder
    {
        public AuditSpyEndpoint() => EndpointSetup<DefaultServer, Context>((config, context) => config.RegisterMessageMutator(new BodySpy(context)));

        class BodySpy(Context testContext) : IMutateIncomingTransportMessages
        {
            public Task MutateIncoming(MutateIncomingTransportMessageContext context)
            {
                testContext.MessageAudited = true;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }

        [Handler]
        public class MessageToBeAuditedHandler : IHandleMessages<ResponseToBeAudited>
        {
            public Task Handle(ResponseToBeAudited message, IMessageHandlerContext context) => Task.CompletedTask;
        }
    }

    public class ResponseToBeAudited : IMessage;

    public class Request : IMessage;
}