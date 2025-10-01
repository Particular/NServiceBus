namespace NServiceBus.AcceptanceTests.Core.Routing.MessageDrivenSubscriptions;

using System.Linq;
using System.Threading.Tasks;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NServiceBus.Features;
using NServiceBus.Logging;
using NUnit.Framework;

public class Missing_pub_info : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_log_events_with_missing_routes()
    {
        Requires.MessageDrivenPubSub();

        var context = await Scenario.Define<ScenarioContext>()
            .WithEndpoint<Subscriber>()
            .Done(c => c.EndpointsStarted)
            .Run();

        Assert.That(context.EndpointsStarted, Is.True, "because it should not prevent endpoint startup");

        var log = context.Logs.Single(l => l.Message.Contains($"AutoSubscribe was unable to subscribe to an event:"));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(log.Level, Is.EqualTo(LogLevel.Error));
            Assert.That(log.LoggerName, Is.EqualTo(typeof(AutoSubscribe).FullName));
        }
        Assert.That(log.Message, Does.Contain($"No publisher address could be found for message type '{typeof(MyEvent).FullName}'."));
    }

    public class Subscriber : EndpointConfigurationBuilder
    {
        public Subscriber()
        {
            EndpointSetup<DefaultServer>();
        }

        public class MyHandler : IHandleMessages<MyEvent>
        {
            public Task Handle(MyEvent @event, IMessageHandlerContext context)
            {
                return Task.CompletedTask;
            }
        }
    }

    public class MyEvent : IEvent
    {
    }
}