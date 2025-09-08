namespace NServiceBus.AcceptanceTests.Outbox;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using AcceptanceTests;
using EndpointTemplates;
using Features;
using NUnit.Framework;

public class When_subscribers_handles_the_same_event : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_be_processed_by_all_subscribers()
    {
        Requires.OutboxPersistence();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<Publisher>(b =>
                b.When(c => c.Subscriber1Subscribed && c.Subscriber2Subscribed, session => session.Publish(new MyEvent()))
            )
            .WithEndpoint<Subscriber1>(b => b.When(async (session, ctx) =>
            {
                await session.Subscribe<MyEvent>();
                if (ctx.HasNativePubSubSupport)
                {
                    ctx.Subscriber1Subscribed = true;
                    ctx.AddTrace("Subscriber1 is now subscribed (at least we have asked the broker to be subscribed)");
                }
                else
                {
                    ctx.AddTrace("Subscriber1 has now asked to be subscribed to MyEvent");
                }
            }))
            .WithEndpoint<Subscriber2>(b => b.When(async (session, ctx) =>
            {
                await session.Subscribe<MyEvent>();
                if (ctx.HasNativePubSubSupport)
                {
                    ctx.Subscriber2Subscribed = true;
                    ctx.AddTrace("Subscriber2 is now subscribed (at least we have asked the broker to be subscribed)");
                }
                else
                {
                    ctx.AddTrace("Subscriber2 has now asked to be subscribed to MyEvent");
                }
            }))
            .Done(c => c.Subscriber1GotTheEvent && c.Subscriber2GotTheEvent)
            .Run(TimeSpan.FromSeconds(10));

        Assert.Multiple(() =>
        {
            Assert.That(context.Subscriber1GotTheEvent, Is.True);
            Assert.That(context.Subscriber2GotTheEvent, Is.True);
        });
    }

    public class Context : ScenarioContext
    {
        public bool Subscriber1Subscribed { get; set; }
        public bool Subscriber2Subscribed { get; set; }

        public bool Subscriber1GotTheEvent { get; set; }
        public bool Subscriber2GotTheEvent { get; set; }
    }

    public class Publisher : EndpointConfigurationBuilder
    {
        public Publisher() => EndpointSetup<DefaultPublisher>(c =>
        {
            c.OnEndpointSubscribed<Context>((s, context) =>
            {
                var subscriber1 = Conventions.EndpointNamingConvention(typeof(Subscriber1));
                if (s.SubscriberEndpoint.Contains(subscriber1))
                {
                    context.Subscriber1Subscribed = true;
                    context.AddTrace($"{subscriber1} is now subscribed");
                }

                var subscriber2 = Conventions.EndpointNamingConvention(typeof(Subscriber2));
                if (s.SubscriberEndpoint.Contains(subscriber2))
                {
                    context.Subscriber2Subscribed = true;
                    context.AddTrace($"{subscriber2} is now subscribed");
                }
            });
        }, metadata => metadata.RegisterSelfAsPublisherFor<MyEvent>(this));
    }

    public class Subscriber1 : EndpointConfigurationBuilder
    {
        public Subscriber1() =>
            EndpointSetup<DefaultServer>(c =>
            {
                c.DisableFeature<AutoSubscribe>();

                c.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;
                c.EnableOutbox();
            }, metadata => metadata.RegisterPublisherFor<MyEvent>(typeof(Publisher)));

        public class MyHandler(Context testContext) : IHandleMessages<MyEvent>
        {
            public Task Handle(MyEvent message, IMessageHandlerContext context)
            {
                testContext.Subscriber1GotTheEvent = true;
                return Task.CompletedTask;
            }
        }
    }

    public class Subscriber2 : EndpointConfigurationBuilder
    {
        public Subscriber2() =>
            EndpointSetup<DefaultServer>(c =>
            {
                c.DisableFeature<AutoSubscribe>();

                c.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;
                c.EnableOutbox();
            }, metadata => metadata.RegisterPublisherFor<MyEvent>(typeof(Publisher)));

        public class MyHandler(Context testContext) : IHandleMessages<MyEvent>
        {
            public Task Handle(MyEvent messageThatIsEnlisted, IMessageHandlerContext context)
            {
                testContext.Subscriber2GotTheEvent = true;
                return Task.CompletedTask;
            }
        }
    }

    public class MyEvent : IEvent;
}