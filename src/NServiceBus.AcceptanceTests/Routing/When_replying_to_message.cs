namespace NServiceBus.AcceptanceTests.Routing;

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
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ctx.SendingEndpointGotResponse, Is.True);
            Assert.That(ctx.OtherEndpointGotResponse, Is.False);
        }
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
            .Run();

        Assert.That(ctx.SendingEndpointGotResponse, Is.True);
        Assert.That(ctx.ReplyToAddress, Does.Not.Contain(instanceDiscriminator));
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
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ctx.OtherEndpointGotResponse, Is.True);
            Assert.That(ctx.SendingEndpointGotResponse, Is.False);
        }
    }

    public class Context : ScenarioContext
    {
        public bool SendingEndpointGotResponse { get; set; }
        public bool OtherEndpointGotResponse { get; set; }
        public string ReplyToAddress { get; set; }
    }

    public class SendingEndpoint : EndpointConfigurationBuilder
    {
        public SendingEndpoint() =>
            EndpointSetup<DefaultServer>(c =>
            {
                c.ConfigureRouting().RouteToEndpoint(typeof(MyMessage), typeof(ReplyingEndpoint));
            });

        public class ResponseHandler(Context testContext) : IHandleMessages<MyReply>
        {
            public Task Handle(MyReply messageThatIsEnlisted, IMessageHandlerContext context)
            {
                testContext.SendingEndpointGotResponse = true;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class OtherEndpoint : EndpointConfigurationBuilder
    {
        public OtherEndpoint() => EndpointSetup<DefaultServer>();

        public class ResponseHandler(Context testContext) : IHandleMessages<MyReply>
        {
            public Task Handle(MyReply messageThatIsEnlisted, IMessageHandlerContext context)
            {
                testContext.OtherEndpointGotResponse = true;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class ReplyingEndpoint : EndpointConfigurationBuilder
    {
        public ReplyingEndpoint() => EndpointSetup<DefaultServer>();

        public class MessageHandler(Context testContext) : IHandleMessages<MyMessage>
        {
            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                testContext.ReplyToAddress = context.ReplyToAddress;
                return context.Reply(new MyReply());
            }
        }
    }

    public class MyMessage : IMessage;

    public class MyReply : IMessage;
}