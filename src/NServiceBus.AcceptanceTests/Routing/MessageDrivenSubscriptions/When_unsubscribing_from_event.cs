namespace NServiceBus.AcceptanceTests.Routing.MessageDrivenSubscriptions
{
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    public class When_unsubscribing_from_event : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_no_longer_receive_event()
        {
            Requires.MessageDrivenPubSub();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Publisher>(c => c
                    .When(
                        ctx => ctx.Subscriber1Subscribed && ctx.Subscriber2Subscribed,
                        s => s.Publish(new Event()))
                    .When(
                        ctx => ctx.Subscriber2Unsubscribed,
                        async s =>
                        {
                            await s.Publish(new Event());
                            await s.Publish(new Event());
                            await s.Publish(new Event());
                        }))
                .WithEndpoint<Subscriber1>(c => c
                    .When(s => s.Subscribe<Event>()))
                .WithEndpoint<Subscriber2>(c => c
                    .When(s => s.Subscribe<Event>())
                    .When(
                        ctx => ctx.Subscriber2ReceivedMessages >= 1,
                        s => s.Unsubscribe<Event>()))
                .Done(c => c.Subscriber1ReceivedMessages >= 4)
                .Run();

            Assert.AreEqual(4, context.Subscriber1ReceivedMessages);
            Assert.AreEqual(1, context.Subscriber2ReceivedMessages);
            Assert.IsTrue(context.Subscriber2Unsubscribed);
        }

        public class Context : ScenarioContext
        {
            public bool Subscriber1Subscribed { get; set; }
            public bool Subscriber2Subscribed { get; set; }
            public bool Subscriber2Unsubscribed { get; set; }
            public int Subscriber1ReceivedMessages;
            public int Subscriber2ReceivedMessages;
        }

        public class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.OnEndpointSubscribed<Context>((args, ctx) =>
                    {
                        if (args.SubscriberEndpoint.Contains(Conventions.EndpointNamingConvention(typeof(Subscriber1))))
                        {
                            ctx.Subscriber1Subscribed = true;
                        }

                        if (args.SubscriberEndpoint.Contains(Conventions.EndpointNamingConvention(typeof(Subscriber2))))
                        {
                            ctx.Subscriber2Subscribed = true;
                        }
                    });
                    c.OnEndpointUnsubscribed<Context>((args, ctx) =>
                    {
                        if (args.SubscriberEndpoint.Contains(Conventions.EndpointNamingConvention(typeof(Subscriber2))))
                        {
                            ctx.Subscriber2Unsubscribed = true;
                        }
                    });
                });
            }
        }

        public class Subscriber1 : EndpointConfigurationBuilder
        {
            public Subscriber1()
            {
                 EndpointSetup<DefaultServer>(c =>
                    {
                        c.DisableFeature<AutoSubscribe>();
                        c.LimitMessageProcessingConcurrencyTo(1);
                    },
                    metadata => metadata.RegisterPublisherFor<Event>(typeof(Publisher)));
            }

            public class EventHandler : IHandleMessages<Event>
            {
                public EventHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(Event message, IMessageHandlerContext context)
                {
                    Interlocked.Increment(ref testContext.Subscriber1ReceivedMessages);
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class Subscriber2 : EndpointConfigurationBuilder
        {
            public Subscriber2()
            {
                EndpointSetup<DefaultServer>(c =>
                    {
                        c.DisableFeature<AutoSubscribe>();
                        c.LimitMessageProcessingConcurrencyTo(1);
                    },
                    metadata => metadata.RegisterPublisherFor<Event>(typeof(Publisher)));
            }

            public class EventHandler : IHandleMessages<Event>
            {
                public EventHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(Event message, IMessageHandlerContext context)
                {
                    Interlocked.Increment(ref testContext.Subscriber2ReceivedMessages);
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class Event : IEvent
        {
        }
    }
}
