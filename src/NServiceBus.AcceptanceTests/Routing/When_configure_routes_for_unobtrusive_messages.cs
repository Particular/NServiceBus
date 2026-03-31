namespace NServiceBus.AcceptanceTests.Routing;

using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using Configuration.AdvancedExtensibility;
using EndpointTemplates;
using NUnit.Framework;

public class When_configure_routes_for_unobtrusive_messages : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_use_routes_from_routing_api()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<SendingEndpointUsingRoutingApi>(e => e
                .When(s => s.Send(new SomeCommand())))
            .WithEndpoint<ReceivingEndpoint>()
            .Run();

        Assert.That(context.ReceivedMessage, Is.True);
    }

    public class Context : ScenarioContext
    {
        public bool ReceivedMessage { get; set; }
    }

    public class SendingEndpointUsingRoutingApi : EndpointConfigurationBuilder
    {
        public SendingEndpointUsingRoutingApi() =>
            EndpointSetup<DefaultServer>(c =>
            {
                c.Conventions().DefiningCommandsAs(t => t == typeof(SomeCommand));

                var routing = new RoutingSettings(c.GetSettings());
                routing.RouteToEndpoint(typeof(SomeCommand).Assembly, Conventions.EndpointNamingConvention(typeof(ReceivingEndpoint)));
            });
    }

    public class ReceivingEndpoint : EndpointConfigurationBuilder
    {
        public ReceivingEndpoint() =>
            EndpointSetup<DefaultServer>(c => c
                .Conventions()
                .DefiningCommandsAs(t => t == typeof(SomeCommand)));

        [Handler]
        public class CommandHandler(Context testContext) : IHandleMessages<SomeCommand>
        {
            public Task Handle(SomeCommand message, IMessageHandlerContext context)
            {
                testContext.ReceivedMessage = true;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class SomeCommand
    {
    }
}