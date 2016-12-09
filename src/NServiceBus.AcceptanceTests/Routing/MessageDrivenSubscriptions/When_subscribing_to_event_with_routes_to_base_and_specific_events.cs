namespace NServiceBus.AcceptanceTests.Routing.MessageDrivenSubscriptions
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_subscribing_to_event_with_routes_to_base_and_specific_events : NServiceBusAcceptanceTest
    {
        [Test]
        public Task Event_from_both_publishers_should_be_delivered()
        {
            return Scenario.Define<Context>()
                .WithEndpoint<PublisherOne>(b => b.When(c => c.SubscriberSubscribedToOne, async session =>
                {
                    await session.Publish(new EventOne());
                }))
                .WithEndpoint<PublisherTwo>(b => b.When(c => c.SubscriberSubscribedToTwo, async session =>
                {
                    await session.Publish(new EventTwo());
                }))
                .WithEndpoint<Subscriber>(b => b.When(async (session, c) => await session.Subscribe<IBaseEvent>()))
                .Done(c => c.SubscriberGotEventOne)
                .Repeat(r => r.For<AllTransportsWithMessageDrivenPubSub>())
                .Should(c => Assert.IsTrue(c.SubscriberGotEventOne))
                .Run();
        }

        public class Context : ScenarioContext
        {
            public bool SubscriberGotEventOne { get; set; }
            public bool SubscriberSubscribedToOne { get; set; }
            public bool SubscriberSubscribedToTwo { get; set; }
        }

        public class PublisherOne : EndpointConfigurationBuilder
        {
            public PublisherOne()
            {
                EndpointSetup<DefaultPublisher>(b => b.OnEndpointSubscribed<Context>((args, context) =>
                {
                    context.SubscriberSubscribedToOne = true;
                }));
            }
        }

        public class PublisherTwo : EndpointConfigurationBuilder
        {
            public PublisherTwo()
            {
                EndpointSetup<DefaultPublisher>(b => b.OnEndpointSubscribed<Context>((args, context) =>
                {
                    context.SubscriberSubscribedToTwo = true;
                }));
            }
        }

        public class Subscriber : EndpointConfigurationBuilder
        {
            public Subscriber()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.DisableFeature<AutoSubscribe>();
                })
                    .AddMapping<EventTwo>(typeof(PublisherTwo))
                    .AddMapping<IBaseEvent>(typeof(PublisherOne));
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