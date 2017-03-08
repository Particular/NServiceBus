// disable obsolete warnings. Test will be removed in next major version
#pragma warning disable CS0618
namespace NServiceBus.AcceptanceTests.Routing.MessageDrivenSubscriptions
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using Features;
    using NServiceBus.Config;
    using NUnit.Framework;

    public class When_subscribing_to_a_base_event_with_a_route_for_a_derived_event : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Event_should_be_delivered()
        {
            Requires.MessageDrivenPubSub();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<PublisherOne>(b => b.When(c => c.SubscriberSubscribedToOne, async session => { await session.Publish(new EventOne()); }))
                .WithEndpoint<PublisherTwo>(b => b.When(c => c.SubscriberSubscribedToTwo, async session => { await session.Publish(new EventTwo()); }))
                .WithEndpoint<Subscriber>(b => b.When(async (session, c) => await session.Subscribe<IBaseEvent>()))
                .Done(c => c.SubscriberGotEventOne)
                .Run();

            Assert.IsTrue(context.SubscriberGotEventOne);
        }

        public class Context : ScenarioContext
        {
            public bool SubscriberGotEventOne { get; set; }
            public bool SubscriberGotEventTwo { get; set; }

            public bool SubscriberSubscribedToOne { get; set; }
            public bool SubscriberSubscribedToTwo { get; set; }
        }

        public class PublisherOne : EndpointConfigurationBuilder
        {
            public PublisherOne()
            {
                EndpointSetup<DefaultPublisher>(b => b.OnEndpointSubscribed<Context>((args, context) => { context.SubscriberSubscribedToOne = true; }));
            }
        }

        public class PublisherTwo : EndpointConfigurationBuilder
        {
            public PublisherTwo()
            {
                EndpointSetup<DefaultPublisher>(b => b.OnEndpointSubscribed<Context>((args, context) => { context.SubscriberSubscribedToTwo = true; }));
            }
        }

        public class Subscriber : EndpointConfigurationBuilder
        {
            public Subscriber()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.DisableFeature<AutoSubscribe>();
                }).WithConfig<UnicastBusConfig>(u =>
                {
                    u.MessageEndpointMappings.Add(new MessageEndpointMapping
                    {
                        AssemblyName = typeof(EventOne).Assembly.FullName,
                        TypeFullName = typeof(EventOne).FullName,
                        Endpoint = Conventions.EndpointNamingConvention(typeof(PublisherOne))
                    });
                    u.MessageEndpointMappings.Add(new MessageEndpointMapping
                    {
                        AssemblyName = typeof(EventTwo).Assembly.FullName,
                        TypeFullName = typeof(EventTwo).FullName,
                        Endpoint = Conventions.EndpointNamingConvention(typeof(PublisherTwo))
                    });
                });
            }

            public class MyEventHandler : IHandleMessages<IBaseEvent>
            {
                public Context Context { get; set; }

                public Task Handle(IBaseEvent messageThatIsEnlisted, IMessageHandlerContext context)
                {
                    if (messageThatIsEnlisted is EventOne)
                    {
                        Context.SubscriberGotEventOne = true;
                    }
                    if (messageThatIsEnlisted is EventTwo)
                    {
                        Context.SubscriberGotEventTwo = true;
                    }
                    return Task.FromResult(0);
                }
            }
        }

        public class EventOne : IBaseEvent
        {
        }

        public class EventTwo : IBaseEvent
        {
        }

        public interface IBaseEvent : IEvent
        {
        }
    }
}
#pragma warning restore CS0618