namespace NServiceBus.AcceptanceTests.Core.Hosting;

using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_scanning_is_enabled : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_discover_handlers()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<MyEndpoint>(c => c
                .When(b => b.SendLocal(new MyMessage())))
            .Done(c => c.GotTheMessage)
            .Run();

        Assert.That(context.GotTheMessage, Is.True);
    }

    class Context : ScenarioContext
    {
        public bool GotTheMessage { get; set; }
    }

    class MyEndpoint : EndpointConfigurationBuilder
    {
        public MyEndpoint() =>
            EndpointSetup<DefaultServer>()
                .DoNotAutoRegisterHandlers()
                .IncludeType<MyMessageHandler>();

        public class MyMessageHandler(Context testContext) : IHandleMessages<MyMessage>
        {
            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                testContext.GotTheMessage = true;
                return Task.CompletedTask;
            }
        }
    }

    class MyMessage : IMessage;
}