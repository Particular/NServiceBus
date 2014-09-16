namespace NServiceBus.AcceptanceTests.PubSub
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using Features;
    using NUnit.Framework;

    public class When_subscribing_to_a_polymorphic_event : NServiceBusAcceptanceTest
    {
        [Test]
        public void Event_should_be_delivered()
        {
            var cc = new Context();

            Scenario.Define(cc)
                    .WithEndpoint<Publisher>(b => b.When(c => c.Subscriber1Subscribed && c.Subscriber2Subscribed, bus => bus.Publish(new MyEvent())))
                    .WithEndpoint<Subscriber1>(b => b.Given((bus, context) =>
                        {
                            bus.Subscribe<IMyEvent>();

                            if (context.HasNativePubSubSupport)
                                context.Subscriber1Subscribed = true;
                        }))
                    .WithEndpoint<Subscriber2>(b => b.Given((bus, context) =>
                        {
                            bus.Subscribe<MyEvent>();

                            if (context.HasNativePubSubSupport)
                                context.Subscriber2Subscribed = true;
                        }))
                    .Done(c => c.Subscriber1GotTheEvent && c.Subscriber2GotTheEvent)
                    .Run();

            Assert.True(cc.Subscriber1GotTheEvent);
            Assert.True(cc.Subscriber2GotTheEvent);
        }

        public class Context : ScenarioContext
        {
            public bool Subscriber1GotTheEvent { get; set; }

            public bool Subscriber2GotTheEvent { get; set; }

            public int NumberOfSubscribers { get; set; }

            public bool Subscriber1Subscribed { get; set; }

            public bool Subscriber2Subscribed { get; set; }
        }

        public class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultPublisher>(b => b.OnEndpointSubscribed<Context>((args, context) =>
                {
                    if (args.SubscriberReturnAddress.Queue.Contains("Subscriber1"))
                    {
                        context.Subscriber1Subscribed = true;
                    }

                    if (args.SubscriberReturnAddress.Queue.Contains("Subscriber2"))
                    {
                        context.Subscriber2Subscribed = true;
                    }
                }));
            }
        }

        public class Subscriber1 : EndpointConfigurationBuilder
        {
            public Subscriber1()
            {
                EndpointSetup<DefaultServer>(c => c.DisableFeature<AutoSubscribe>())
                    .AddMapping<IMyEvent>(typeof(Publisher));
            }

            public class MyEventHandler : IHandleMessages<IMyEvent>
            {
                public Context Context { get; set; }

                public void Handle(IMyEvent messageThatIsEnlisted)
                {
                    Context.Subscriber1GotTheEvent = true;
                }
            }
        }

        public class Subscriber2 : EndpointConfigurationBuilder
        {
            public Subscriber2()
            {
                EndpointSetup<DefaultServer>(c => c.DisableFeature<AutoSubscribe>())
                        .AddMapping<MyEvent>(typeof(Publisher));
            }

            public class MyEventHandler : IHandleMessages<MyEvent>
            {
                public Context Context { get; set; }

                public void Handle(MyEvent messageThatIsEnlisted)
                {
                    Context.Subscriber2GotTheEvent = true;
                }
            }
        }
        
        [Serializable]
        public class MyEvent : IMyEvent
        {
        }

        public interface IMyEvent : IEvent
        {
        }
    }
}