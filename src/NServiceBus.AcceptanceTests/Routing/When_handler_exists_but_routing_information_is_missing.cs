namespace NServiceBus.AcceptanceTests.Routing
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class when_using_autosubscribe_with_missing_routing_information : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_skip_events_with_missing_routes()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<Subscriber>()
                .Done(c => c.EndpointsStarted)
                .Repeat(r => r.For<AllTransportsWithMessageDrivenPubSub>())
                .Should(c => { Assert.True(c.EndpointsStarted); })
                .Run();
        }

        public class Context : ScenarioContext
        {
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