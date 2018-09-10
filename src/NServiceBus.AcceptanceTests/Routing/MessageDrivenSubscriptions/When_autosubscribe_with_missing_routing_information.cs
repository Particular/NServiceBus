namespace NServiceBus.AcceptanceTests.Routing.MessageDrivenSubscriptions
{
    using AcceptanceTesting;
    using EndpointTemplates;
    using Logging;
    using NUnit.Framework;
    using System.Linq;
    using System.Threading.Tasks;

    public class When_autosubscribe_with_missing_routing_information : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_log_events_with_missing_routes()
        {
            Requires.MessageDrivenPubSub();

            var context = await Scenario.Define<ScenarioContext>()
                .WithEndpoint<Subscriber>()
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.True(context.EndpointsStarted, "because it should not prevent endpoint startup");

            var log = context.Logs.Single(l => l.Message.Contains($"AutoSubscribe was unable to subscribe to event '{typeof(MyEvent).FullName}': No publisher address could be found for message type '{typeof(MyEvent).FullName}'."));
            Assert.AreEqual(LogLevel.Error, log.Level);
        }

        public class Subscriber : EndpointConfigurationBuilder
        {
            public Subscriber()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MyEventHandler : IHandleMessages<MyEvent>
            {
                public Task Handle(MyEvent @event, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }
            }
        }

        public class MyEvent : IEvent
        {
        }
    }
}