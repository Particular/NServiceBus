namespace NServiceBus.AcceptanceTests.Routing
{
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class When_publishing_an_interface : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_receive_event_for_non_xml()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<Publisher>(b =>
                    b.When(c => c.Subscribed, (bus, ctx) => bus.Publish<MyEvent>()))
                .WithEndpoint<Subscriber>(b => b.When(async (bus, context) =>
                {
                    await bus.Subscribe<MyEvent>();
                    if (context.HasNativePubSubSupport)
                    {
                        context.Subscribed = true;
                    }
                }))
                .Done(c => c.GotTheEvent)
                .Repeat(r => r.For(Serializers.Json))
                .Should(c =>
                {
                    Assert.True(c.GotTheEvent);
                })
                .Run();
        }

        public class Context : ScenarioContext
        {
            public bool GotTheEvent { get; set; }
            public bool Subscribed { get; set; }
        }

        public class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultPublisher>(b => b.OnEndpointSubscribed<Context>((s, context) =>
                {
                    if (s.SubscriberReturnAddress.Contains("Subscriber"))
                    {
                        if (s.SubscriberReturnAddress.Contains("Subscriber"))
                        {
                            context.Subscribed = true;
                        }
                    }
                }));
            }
        }

        public class Subscriber : EndpointConfigurationBuilder
        {
            public Subscriber()
            {
                EndpointSetup<DefaultServer>(c => c.DisableFeature<AutoSubscribe>())
                    .AddMapping<MyEvent>(typeof(Publisher));
            }

            public class MyEventHandler : IHandleMessages<MyEvent>
            {
                public Context Context { get; set; }

                public Task Handle(MyEvent @event, IMessageHandlerContext context)
                {
                    Context.GotTheEvent = true;
                    return Task.FromResult(0);
                }
            }
        }

        public interface MyEvent : IEvent
        {
        }
    }
}