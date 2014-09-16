namespace NServiceBus.AcceptanceTests.Versioning
{
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Features;
    using NUnit.Framework;
    using PubSub;

    public class When_multiple_versions_of_a_message_is_published : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_deliver_is_to_both_v1_and_vX_subscribers()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<V2Publisher>(b =>
                        b.When(c => c.V1Subscribed && c.V2Subscribed, (bus, c) => bus.Publish<V2Event>(e =>
                                 {
                                     e.SomeData = 1;
                                     e.MoreInfo = "dasd";
                                 })))
                    .WithEndpoint<V1Subscriber>(b => b.Given((bus,c) =>
                        {
                            bus.Subscribe<V1Event>();
                            if (c.HasNativePubSubSupport)
                                c.V1Subscribed = true;
                        }))
                    .WithEndpoint<V2Subscriber>(b => b.Given((bus,c) =>
                        {
                            bus.Subscribe<V2Event>();
                            if (c.HasNativePubSubSupport)
                                c.V2Subscribed = true;
                        }))
                    .Done(c => c.V1SubscriberGotTheMessage && c.V2SubscriberGotTheMessage)
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public bool V1SubscriberGotTheMessage { get; set; }

            public bool V2SubscriberGotTheMessage { get; set; }

            public bool V1Subscribed { get; set; }

            public bool V2Subscribed { get; set; }
        }

        public class V2Publisher : EndpointConfigurationBuilder
        {
            public V2Publisher()
            {
                EndpointSetup<DefaultPublisher>(b => b.OnEndpointSubscribed<Context>((s, context) =>
                {
                    if (s.SubscriberReturnAddress.Queue.Contains("V1Subscriber"))
                    {
                        context.V1Subscribed = true;
                    }

                    if (s.SubscriberReturnAddress.Queue.Contains("V2Subscriber"))
                    {
                        context.V2Subscribed = true;
                    }
                }));
            }
        }
        public class V1Subscriber : EndpointConfigurationBuilder
        {
            public V1Subscriber()
            {
                EndpointSetup<DefaultServer>(b => b.DisableFeature<AutoSubscribe>())
                    .ExcludeType<V2Event>()
                    .AddMapping<V1Event>(typeof(V2Publisher));

            }


            class V1Handler:IHandleMessages<V1Event>
            {
                public Context Context { get; set; }
                public void Handle(V1Event message)
                {
                    Context.V1SubscriberGotTheMessage = true;
                }
            }
        }


        public class V2Subscriber : EndpointConfigurationBuilder
        {
            public V2Subscriber()
            {
                EndpointSetup<DefaultServer>(b => b.DisableFeature<AutoSubscribe>())
                     .AddMapping<V2Event>(typeof(V2Publisher));
            }

            class V2Handler : IHandleMessages<V2Event>
            {
                public Context Context { get; set; }

                public void Handle(V2Event message)
                {
                    Context.V2SubscriberGotTheMessage = true;
                }
            }
        }


        public interface V1Event : IEvent
        {
            int SomeData { get; set; }
        }

        public interface V2Event : V1Event
        {
            string MoreInfo { get; set; }
        }
    }
}
