namespace NServiceBus.AcceptanceTests.Versioning
{
    using System.Threading.Tasks;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.AcceptanceTests.Routing;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class When_multiple_versions_of_a_message_is_published : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_deliver_is_to_both_v1_and_vX_subscribers()
        {
            await Scenario.Define<Context>()
                    .WithEndpoint<V2Publisher>(b =>
                        b.When(c => c.V1Subscribed && c.V2Subscribed, (bus, c) =>
                        {
                            return bus.Publish<V2Event>(e =>
                            {
                                e.SomeData = 1;
                                e.MoreInfo = "dasd";
                            });
                        }))
                    .WithEndpoint<V1Subscriber>(b => b.When(async (bus,c) =>
                        {
                            await bus.Subscribe<V1Event>();
                            if (c.HasNativePubSubSupport)
                                c.V1Subscribed = true;
                        }))
                    .WithEndpoint<V2Subscriber>(b => b.When(async (bus,c) =>
                        {
                            await bus.Subscribe<V2Event>();
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
                    if (s.SubscriberReturnAddress.Contains("V1Subscriber"))
                    {
                        context.V1Subscribed = true;
                    }

                    if (s.SubscriberReturnAddress.Contains("V2Subscriber"))
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
                public Task Handle(V1Event message, IMessageHandlerContext context)
                {
                    Context.V1SubscriberGotTheMessage = true;
                    return Task.FromResult(0);
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

                public Task Handle(V2Event message, IMessageHandlerContext context)
                {
                    Context.V2SubscriberGotTheMessage = true;
                    return Task.FromResult(0);
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
