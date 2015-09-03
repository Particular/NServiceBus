namespace NServiceBus.AcceptanceTests.Routing
{
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class When_publishing_an_interface : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_receive_event_for_non_xml()
        {
            Scenario.Define<Context>()
                .WithEndpoint<Publisher>(b =>
                    b.When(c => c.Subscribed, bus => bus.Publish<MyEvent>())
                )
                .WithEndpoint<Subscriber>(b => b.Given((bus, context) =>
                {
                    bus.Subscribe<MyEvent>();
                    if (context.HasNativePubSubSupport)
                    {
                        context.Subscribed = true;
                    }
                }))
                .Done(c => c.GotTheEvent)
                .Repeat(r => r.For(Serializers.Json))
                .Should(c => Assert.True(c.GotTheEvent))
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
                        context.Subscribed = true;
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

                public void Handle(MyEvent messageThatIsEnlisted)
                {
                    Context.GotTheEvent = true;
                }
            }

        }

        public interface MyEvent : IEvent
        {
        }
    }
}