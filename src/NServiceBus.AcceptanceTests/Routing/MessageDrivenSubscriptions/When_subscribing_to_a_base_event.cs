namespace NServiceBus.AcceptanceTests.Routing.MessageDrivenSubscriptions
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_subscribing_to_a_base_event : NServiceBusAcceptanceTest
    {
        [Test]
        public Task Both_base_and_specific_events_should_be_delivered()
        {
            return Scenario.Define<Context>()
                .WithEndpoint<Publisher>(b => b.When(c => c.SubscriberSubscribed, async session =>
                {
                    await session.Publish(new SpecificEvent());
                    await session.Publish<IBaseEvent>();
                }))
                .WithEndpoint<GeneralSubscriber>(b => b.When(async (session, c) => await session.Subscribe<IBaseEvent>()))
                .Done(c => c.SubscriberGotBaseEvent && c.SubscriberGotSpecificEvent)
                .Repeat(r => r.For<AllTransportsWithMessageDrivenPubSub>())
                .Should(c =>
                {
                    Assert.True(c.SubscriberGotBaseEvent);
                    Assert.True(c.SubscriberGotSpecificEvent);
                })
                .Run();
        }

        public class Context : ScenarioContext
        {
            public bool SubscriberGotBaseEvent { get; set; }
            public bool SubscriberGotSpecificEvent { get; set; }
            public bool SubscriberSubscribed { get; set; }
        }

        public class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultPublisher>(b => b.OnEndpointSubscribed<Context>((args, context) =>
                {
                    context.SubscriberSubscribed = true;
                }));
            }
        }

        public class GeneralSubscriber : EndpointConfigurationBuilder
        {
            public GeneralSubscriber()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.DisableFeature<AutoSubscribe>();
                },
                metadata => metadata.RegisterPublisherFor<IBaseEvent>(typeof(Publisher)));
            }

            public class MyEventHandler : IHandleMessages<IBaseEvent>
            {
                public Context Context { get; set; }

                public Task Handle(IBaseEvent messageThatIsEnlisted, IMessageHandlerContext context)
                {
                    if (messageThatIsEnlisted is SpecificEvent)
                    {
                        Context.SubscriberGotSpecificEvent = true;
                    }
                    else
                    {
                        Context.SubscriberGotBaseEvent = true;
                    }
                    return Task.FromResult(0);
                }
            }
        }

        public class SpecificEvent : IBaseEvent
        {
        }

        public interface IBaseEvent : IEvent
        {
        }
    }
}