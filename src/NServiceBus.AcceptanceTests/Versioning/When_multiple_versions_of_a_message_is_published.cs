namespace NServiceBus.AcceptanceTests.Versioning
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;
    using Conventions = AcceptanceTesting.Customization.Conventions;

    public class When_multiple_versions_of_a_message_is_published : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_deliver_is_to_both_v1_and_vX_subscribers()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<V2Publisher>(b =>
                    b.When(c => c.V1Subscribed && c.V2Subscribed, (session, c) =>
                    {
                        return session.Publish<V2Event>(e =>
                        {
                            e.SomeData = 1;
                            e.MoreInfo = "dasd";
                        });
                    }))
                .WithEndpoint<V1Subscriber>(b => b.When(async (session, c) =>
                {
                    await session.Subscribe<V1Event>();
                    if (c.HasNativePubSubSupport)
                    {
                        c.V1Subscribed = true;
                    }
                }))
                .WithEndpoint<V2Subscriber>(b => b.When(async (session, c) =>
                {
                    await session.Subscribe<V2Event>();
                    if (c.HasNativePubSubSupport)
                    {
                        c.V2Subscribed = true;
                    }
                }))
                .Done(c => c.V1SubscriberGotTheMessage && c.V2SubscriberGotTheMessage)
                .Run();

            Assert.True(context.V1SubscriberGotTheMessage);
            Assert.True(context.V2SubscriberGotTheMessage);
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
                    if (s.SubscriberEndpoint.Contains(Conventions.EndpointNamingConvention(typeof(V1Subscriber))))
                    {
                        context.V1Subscribed = true;
                    }

                    if (s.SubscriberEndpoint.Contains(Conventions.EndpointNamingConvention(typeof(V2Subscriber))))
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
                EndpointSetup<DefaultServer>(b => b.DisableFeature<AutoSubscribe>(),
                    metadata => metadata.RegisterPublisherFor<V1Event>(typeof(V2Publisher)))
                    .ExcludeType<V2Event>();
            }

            class V1Handler : IHandleMessages<V1Event>
            {
                public Context Context { get; set; }

                public V1Handler(Context context)
                {
                    Context = context;
                }

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
                EndpointSetup<DefaultServer>(b => b.DisableFeature<AutoSubscribe>(),
                     metadata => metadata.RegisterPublisherFor<V2Event>(typeof(V2Publisher)));
            }

            class V2Handler : IHandleMessages<V2Event>
            {
                public Context Context { get; set; }

                public V2Handler(Context context)
                {
                    Context = context;
                }

                public Task Handle(V2Event message, IMessageHandlerContext context)
                {
                    Context.V2SubscriberGotTheMessage = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class V1Event : IEvent
        {
            public int SomeData { get; set; }
        }

        public class V2Event : V1Event
        {
            public string MoreInfo { get; set; }
        }
    }
}