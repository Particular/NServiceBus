namespace NServiceBus.AcceptanceTests.Routing;

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
            .Run();

        Assert.That(context.ReplyReceived, Is.True);
        Assert.That(context.ReplyToAddress, Does.Contain(replyHandlerAddress));
    }

    class Context : ScenarioContext
    {
        public string ReplyToAddress { get; set; }
        public bool ReplyReceived { get; set; }
    }

    class Sender : EndpointConfigurationBuilder
    {
        public Sender() =>
            EndpointSetup<DefaultServer>(c =>
            {
                c.ConfigureRouting().RouteToEndpoint(typeof(RequestReplyMessage), typeof(Replier));
            });
    }

    class Replier : EndpointConfigurationBuilder
    {
        public Replier() => EndpointSetup<DefaultServer>();

        class RequestReplyMessageHandler(Context testContext) : IHandleMessages<RequestReplyMessage>
        {
            public Task Handle(RequestReplyMessage message, IMessageHandlerContext context)
            {
                testContext.ReplyToAddress = context.ReplyToAddress;
                return context.Reply(new ReplyMessage());
            }
        }
    }

    class ReplyHandler : EndpointConfigurationBuilder
    {
        public ReplyHandler() => EndpointSetup<DefaultServer>();

        class ReplyMessageHandler(Context testContext) : IHandleMessages<ReplyMessage>
        {
            public Task Handle(ReplyMessage message, IMessageHandlerContext context)
            {
                testContext.ReplyReceived = true;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class RequestReplyMessage : ICommand;

    public class ReplyMessage : IMessage;
}