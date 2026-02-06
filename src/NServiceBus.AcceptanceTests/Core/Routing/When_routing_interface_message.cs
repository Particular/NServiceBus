namespace NServiceBus.AcceptanceTests.Core.Routing;

using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using AcceptanceTesting.Customization;
using NUnit.Framework;

public class When_routing_interface_message : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_use_interface_types_route()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(c => c.When(b => b.SendLocal(new StartMessage())))
            .Run();

        Assert.That(context.GotTheMessage, Is.True);
    }

    public class Context : ScenarioContext
    {
        public bool GotTheMessage { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() =>
            EndpointSetup<DefaultServer>((c, r) =>
            {
                c.ConfigureRouting().RouteToEndpoint(typeof(IMyMessage), typeof(Endpoint));
            });

        [Handler]
        public class StartMessageHandler : IHandleMessages<StartMessage>
        {
            public Task Handle(StartMessage message, IMessageHandlerContext context) => context.Send<IMyMessage>(_ => { });
        }

        [Handler]
        public class MyMessageHandler(Context testContext) : IHandleMessages<IMyMessage>
        {
            public Task Handle(IMyMessage message, IMessageHandlerContext context)
            {
                testContext.GotTheMessage = true;

                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class StartMessage : IMessage;

    public interface IMyMessage : IMessage;
}
