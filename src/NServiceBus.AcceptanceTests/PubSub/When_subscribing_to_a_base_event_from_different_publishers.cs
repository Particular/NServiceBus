namespace NServiceBus.AcceptanceTests.PubSub
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using Features;
    using NUnit.Framework;

    public class When_subscribing_to_a_base_event_from_different_publishers : NServiceBusAcceptanceTest
    {
        [Test]
        public void should_receive_events_from_all_publishers()
        {
            var cc = new Context();

            Scenario.Define(cc)
               .WithEndpoint<Publisher1>(b =>
                        b.Given((bus, context) => SubscriptionBehavior.OnEndpointSubscribed(s =>
                        {
                            if (s.SubscriberReturnAddress.Queue.Contains("Subscriber1"))
                                context.SubscribedToPublisher1 = true;
                        }))
                        .When(c => c.SubscribedToPublisher1, bus => bus.Publish(new DerivedEvent1()))
                     )
                .WithEndpoint<Publisher2>(b =>
                        b.Given((bus, context) => SubscriptionBehavior.OnEndpointSubscribed(s =>
                        {
                            if (s.SubscriberReturnAddress.Queue.Contains("Subscriber1"))
                                context.SubscribedToPublisher2 = true;
                        }))
                        .When(c => c.SubscribedToPublisher2, bus => bus.Publish(new DerivedEvent2()))
                     )
               .WithEndpoint<Subscriber1>(b => b.Given((bus, context) =>
               {
                   if (context.HasNativePubSubSupport)
                   {
                       context.SubscribedToPublisher1 = true;
                       context.SubscribedToPublisher2 = true;
                   }
               }))
               .Done(c => c.GotTheEventFromPublisher1 && c.GotTheEventFromPublisher2)
               .Run();

            Assert.True(cc.GotTheEventFromPublisher1);
            Assert.True(cc.GotTheEventFromPublisher2);
        }

        public class Context : ScenarioContext
        {
            public bool GotTheEventFromPublisher1 { get; set; }
            public bool GotTheEventFromPublisher2 { get; set; }
            public bool SubscribedToPublisher1 { get; set; }
            public bool SubscribedToPublisher2 { get; set; }

        }

        public class Publisher1 : EndpointConfigurationBuilder
        {
            public Publisher1()
            {
                EndpointSetup<DefaultServer>();
            }
        }

        public class Publisher2 : EndpointConfigurationBuilder
        {
            public Publisher2()
            {
                EndpointSetup<DefaultServer>();
            }
        }

        public class Subscriber1 : EndpointConfigurationBuilder
        {
            public Subscriber1()
            {
                EndpointSetup<DefaultServer>(c => c.Features(f=>f.Enable<AutoSubscribe>()))
                    .AddMapping<DerivedEvent1>(typeof(Publisher1))
                    .AddMapping<DerivedEvent2>(typeof(Publisher2));
            }

            public class BaseEventHandler : IHandleMessages<BaseEvent>
            {
                public Context Context { get; set; }

                public void Handle(BaseEvent message)
                {
                    if (message.GetType().FullName.Contains("DerivedEvent1"))
                        Context.GotTheEventFromPublisher1 = true;
                    if (message.GetType().FullName.Contains("DerivedEvent2"))
                        Context.GotTheEventFromPublisher2 = true;
                }
            }
        }

        [Serializable]
        public class BaseEvent : IEvent
        {
        }

        [Serializable]
        public class DerivedEvent1 : BaseEvent
        {

        }

        [Serializable]
        public class DerivedEvent2 : BaseEvent
        {

        }
    }
}