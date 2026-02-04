namespace NServiceBus.AcceptanceTests.Core.Diagnostics;

using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_overriding_input_queue_name : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_use_custom_queue_names()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<MyEndpoint>(e => e.When(b => b.SendLocal(new MyMessage())))
            .Run();

        Assert.That(context.InputQueue, Does.StartWith("OverriddenInputQueue"));
    }

    public class MyEndpoint : EndpointConfigurationBuilder
    {
        public MyEndpoint() =>
            EndpointSetup<DefaultServer>((c, d) =>
            {
                c.OverrideLocalAddress("OverriddenInputQueue");
            });
    }

    [Handler]
    public class MyMessageHandler(Context testContext) : IHandleMessages<MyMessage>
    {
        public Task Handle(MyMessage message, IMessageHandlerContext context)
        {
            testContext.InputQueue = context.MessageHeaders[Headers.ReplyToAddress];
            testContext.MarkAsCompleted();
            return Task.CompletedTask;
        }
    }

    public class Context : ScenarioContext
    {
        public string InputQueue { get; set; }
    }

    public class MyMessage : ICommand;
}