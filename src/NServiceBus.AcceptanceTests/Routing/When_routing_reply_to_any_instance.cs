namespace NServiceBus.AcceptanceTests.Routing;

using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using NUnit.Framework;

[TestFixture]
public class When_routing_reply_to_any_instance : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_route_reply_to_shared_queue()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Sender>(e => e
                .When(s =>
                {
                    var options = new SendOptions();
                    options.RouteReplyToAnyInstance();
                    return s.Send(new RequestReplyMessage(), options);
                }))
            .WithEndpoint<Replier>()
            .Run();

        Assert.That(context.ReplyReceived, Is.True);
        Assert.That(context.ReplyToAddress, Does.Not.Contain(instanceDiscriminator));
    }

    const string instanceDiscriminator = "instance-42";

    public class Context : ScenarioContext
    {
        public string ReplyToAddress { get; set; }
        public bool ReplyReceived { get; set; }
    }

    public class Sender : EndpointConfigurationBuilder
    {
        public Sender() =>
            EndpointSetup<DefaultServer>(c =>
            {
                c.MakeInstanceUniquelyAddressable(instanceDiscriminator);
                c.ConfigureRouting().RouteToEndpoint(typeof(RequestReplyMessage), typeof(Replier));
            });

        [Handler]
        public class ReplyMessageHandler(Context testContext) : IHandleMessages<ReplyMessage>
        {
            public Task Handle(ReplyMessage message, IMessageHandlerContext context)
            {
                testContext.ReplyReceived = true;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class Replier : EndpointConfigurationBuilder
    {
        public Replier() => EndpointSetup<DefaultServer>();

        [Handler]
        public class RequestReplyMessageHandler(Context testContext) : IHandleMessages<RequestReplyMessage>
        {
            public Task Handle(RequestReplyMessage message, IMessageHandlerContext context)
            {
                testContext.ReplyToAddress = context.ReplyToAddress;
                return context.Reply(new ReplyMessage());
            }
        }
    }

    public class RequestReplyMessage : ICommand;

    public class ReplyMessage : IMessage;
}