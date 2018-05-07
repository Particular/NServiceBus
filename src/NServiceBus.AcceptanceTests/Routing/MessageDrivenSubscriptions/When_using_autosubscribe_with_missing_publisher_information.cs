namespace NServiceBus.AcceptanceTests.Routing.MessageDrivenSubscriptions
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_using_autosubscribe_with_missing_publisher_information : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_skip_events_with_missing_routes()
        {
            Requires.MessageDrivenPubSub();

            var context = await Scenario.Define<ScenarioContext>()
                .WithEndpoint<Subscriber>()
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.True(context.EndpointsStarted);
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