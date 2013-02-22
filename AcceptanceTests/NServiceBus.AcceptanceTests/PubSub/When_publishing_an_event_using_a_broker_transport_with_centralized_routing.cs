namespace NServiceBus.AcceptanceTests.PubSub
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_publishing_an_event_using_a_broker_transport_with_centralized_routing : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_be_delivered_to_allsubscribers_without_the_need_for_config()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<CentralizedPublisher>(b => b.Given((bus, context) => bus.Publish(new MyEvent())))
                    .WithEndpoint<CentralizedSubscriber1>()
                    .WithEndpoint<CentralizedSubscriber2>()
                    .Done(c => c.Subscriber1GotTheEvent && c.Subscriber2GotTheEvent)
                    .Repeat(r => r.For<AllTransportsWithCentralizedPubSubSupport>())
                    .Should(c =>
                    {
                        Assert.True(c.Subscriber1GotTheEvent);
                        Assert.True(c.Subscriber2GotTheEvent);
                    })

                    .Run();
        }

        public class Context : ScenarioContext
        {
            public bool Subscriber1GotTheEvent { get; set; }

            public bool Subscriber2GotTheEvent { get; set; }

            public int NumberOfSubscribers { get; set; }
        }

        public class CentralizedPublisher : EndpointConfigurationBuilder
        {
            public CentralizedPublisher()
            {
                EndpointSetup<DefaultServer>();
            }
        }

        public class CentralizedSubscriber1 : EndpointConfigurationBuilder
        {
            public CentralizedSubscriber1()
            {
                //todo - remove this when we have added a hook to allow the transports to set this to true by default
                // we also need to rename since AllowSubscribeToSelf is not a good name
                EndpointSetup<DefaultServer>(c => c.UnicastBus().AllowSubscribeToSelf());
            }

            public class MyEventHandler : IHandleMessages<MyEvent>
            {
                public Context Context { get; set; }

                public void Handle(MyEvent messageThatIsEnlisted)
                {
                    Context.Subscriber1GotTheEvent = true;
                }
            }
        }

        public class CentralizedSubscriber2 : EndpointConfigurationBuilder
        {
            public CentralizedSubscriber2()
            {
                //todo - remove this when we have added a hook to allow the transports to set this to true by default
                // we also need to rename since AllowSubscribeToSelf is not a good name
                EndpointSetup<DefaultServer>(c => c.UnicastBus().AllowSubscribeToSelf());
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
        public class MyEvent : IEvent
        {
        }
    }
}