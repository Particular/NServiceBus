namespace NServiceBus.AcceptanceTests.Routing
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;
    using Conventions = AcceptanceTesting.Customization.Conventions;

    public class When_publishing_using_base_type : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Event_should_be_published_using_instance_type()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Publisher>(b =>
                    b.When(c => c.Subscriber1Subscribed, session =>
                    {
                        IMyEvent message = new EventMessage();

                        return session.Publish(message);
                    }))
                .WithEndpoint<Subscriber1>(b => b.When(async (session, ctx) =>
                {
                    await session.Subscribe<EventMessage>();

                    if (ctx.HasNativePubSubSupport)
                    {
                        ctx.Subscriber1Subscribed = true;
                    }
                }))
                .Done(c => c.Subscriber1GotTheEvent)
                .Run(TimeSpan.FromSeconds(20));

            Assert.True(context.Subscriber1GotTheEvent);
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
                    if (s.SubscriberEndpoint.Contains(Conventions.EndpointNamingConvention(typeof(Subscriber1))))
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
                EndpointSetup<DefaultServer>(c => c.DisableFeature<AutoSubscribe>(), p => p.RegisterPublisherFor<EventMessage>(typeof(Publisher)));
            }

            public class MyEventHandler : IHandleMessages<EventMessage>
            {
                public MyEventHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(EventMessage messageThatIsEnlisted, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    testContext.Subscriber1GotTheEvent = true;
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

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