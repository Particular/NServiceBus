namespace NServiceBus.AcceptanceTests.Routing.MessageDrivenSubscriptions
{
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using Logging;
    using NUnit.Framework;

    public class When_missing_publisher_information : NServiceBusAcceptanceTest
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

            var log = context.Logs.Single(l => l.Message.Contains($"AutoSubscribe was unable to subscribe to an event:"));
            Assert.AreEqual(LogLevel.Error, log.Level);
            Assert.AreEqual(typeof(AutoSubscribe).FullName, log.LoggerName);
            StringAssert.Contains($"No publisher address could be found for message type '{typeof(MyEvent).FullName}'.", log.Message);
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