namespace NServiceBus.AcceptanceTests.TimeToBeReceived;

using System;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Features;
using NUnit.Framework;

public class When_TimeToBeReceived_used_with_unobtrusive_mode : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Message_should_not_be_received()
    {
        var start = DateTime.UtcNow;

        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>()
            .Done(c => c.WasCalled || DateTime.UtcNow - start > TimeSpan.FromSeconds(15))
            .Run();

        Assert.That(context.WasCalled, Is.False);
    }

    public class Context : ScenarioContext
    {
        public bool WasCalled { get; set; }
    }

    class SendMessageAndDelayStartTask : FeatureStartupTask
    {
        protected override async Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
        {
            await session.SendLocal(new MyCommand(), cancellationToken);
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        }

        protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() =>
            EndpointSetup<DefaultServer>(c =>
            {
                c.Conventions()
                    .DefiningCommandsAs(t => t.Namespace != null && t.FullName == typeof(MyCommand).FullName)
                    .DefiningTimeToBeReceivedAs(messageType => messageType == typeof(MyCommand) ? TimeSpan.FromSeconds(2) : TimeSpan.MaxValue);
                c.RegisterStartupTask(new SendMessageAndDelayStartTask());
            });

        public class MyMessageHandler(Context testContext) : IHandleMessages<MyCommand>
        {
            public Task Handle(MyCommand message, IMessageHandlerContext context)
            {
                testContext.WasCalled = true;
                return Task.CompletedTask;
            }
        }
    }

    public class MyCommand;
}