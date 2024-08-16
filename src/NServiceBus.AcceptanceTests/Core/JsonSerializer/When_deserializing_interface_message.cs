namespace NServiceBus.AcceptanceTests.Core.JsonSerializer;

using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_deserializing_interface_message : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_work()
    {
        var context = await Scenario.Define<Context>()
           .WithEndpoint<Endpoint>(c => c
               .When(b => b.SendLocal<IMyMessage>(_ => { })))
           .Done(c => c.GotTheMessage)
           .Run();

        Assert.That(context.GotTheMessage, Is.True);
    }

    public class Context : ScenarioContext
    {
        public bool GotTheMessage { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint()
        {
            EndpointSetup<DefaultServer>((c, r) =>
            {
                c.UseSerialization<SystemJsonSerializer>();
            });
        }

        class MyHandler : IHandleMessages<IMyMessage>
        {

            Context testContext;

            public MyHandler(Context testContext) => this.testContext = testContext;

            public Task Handle(IMyMessage message, IMessageHandlerContext context)
            {
                testContext.GotTheMessage = true;
                return Task.CompletedTask;
            }
        }
    }

    public interface IMyMessage
    {
    }
}