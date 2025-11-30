namespace NServiceBus.AcceptanceTests.TimeToBeReceived;

using System;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Features;
using NUnit.Framework;

public class When_TimeToBeReceived_has_expired : NServiceBusAcceptanceTest
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

    class DelayReceiverFromStartingTask : FeatureStartupTask
    {
        protected override async Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
        {
            await session.SendLocal(new MyMessage(), cancellationToken: cancellationToken);
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        }

        protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() => EndpointSetup<DefaultServer>(c => c.RegisterStartupTask(new DelayReceiverFromStartingTask()));

        public class MyMessageHandler(Context testContext) : IHandleMessages<MyMessage>
        {
            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                testContext.WasCalled = true;
                return Task.CompletedTask;
            }
        }
    }

    [TimeToBeReceived("00:00:02")]
    public class MyMessage : IMessage;
}