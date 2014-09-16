namespace NServiceBus.AcceptanceTests.PubSub
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using Features;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_publishing_using_root_type : NServiceBusAcceptanceTest
    {
        [Test]
        public void Event_should_be_published_using_instance_type()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<Publisher>(b =>
                        b.When(c => c.Subscriber1Subscribed, bus =>
                        {
                            IMyEvent message = new EventMessage();

                            bus.Publish(message);
                        }))
                    .WithEndpoint<Subscriber1>(b => b.Given((bus, context) =>
                    {
                        bus.Subscribe<EventMessage>();

                        if (context.HasNativePubSubSupport)
                        {
                            context.Subscriber1Subscribed = true;
                        }
                    }))
                    .Done(c => c.Subscriber1GotTheEvent)
                    .Repeat(r => r.For(Transports.Default))
                    .Should(c => Assert.True(c.Subscriber1GotTheEvent))
                    .Run(TimeSpan.FromSeconds(20));
        }

        public class Context : ScenarioContext
        {
            public bool Subscriber1GotTheEvent { get; set; }
            public bool Subscriber1Subscribed { get; set; }
        }

        public class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultPublisher>(b => b.OnEndpointSubscribed<Context>((s, context) =>
                {
                    if (s.SubscriberReturnAddress.Queue.Contains("Subscriber1"))
                    {
                        context.Subscriber1Subscribed = true;
                    }
                }));
            }
        }
        
        public class Subscriber1 : EndpointConfigurationBuilder
        {
            public Subscriber1()
            {
                EndpointSetup<DefaultServer>(c => c.DisableFeature<AutoSubscribe>())
                    .AddMapping<EventMessage>(typeof(Publisher));
            }

            public class MyEventHandler : IHandleMessages<EventMessage>
            {
                public Context Context { get; set; }

                public void Handle(EventMessage messageThatIsEnlisted)
                {
                    Context.Subscriber1GotTheEvent = true;
                }
            }
        }

        [Serializable]
        public class EventMessage : IMyEvent
        {
            public Guid EventId { get; set; }
            public DateTime? Time { get; set; }
            public TimeSpan Duration { get; set; }
        }

        public interface IMyEvent : IEvent
        {
            Guid EventId { get; set; }
            DateTime? Time { get; set; }
            TimeSpan Duration { get; set; }
        }
    }
}