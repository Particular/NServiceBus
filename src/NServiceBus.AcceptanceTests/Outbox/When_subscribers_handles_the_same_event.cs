namespace NServiceBus.AcceptanceTests.Outbox;

using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using AcceptanceTests;
using EndpointTemplates;
using Features;
using NUnit.Framework;

public class When_outbox_is_used_by_multiple_subscribers_for_the_same_event : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Each_subscriber_should_dispatch_its_own_transport_operations()
    {
        Requires.OutboxPersistence();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<Publisher>(b => b.When(
                c => c.Subscriber1Subscribed && c.Subscriber2Subscribed,
                session => session.Publish(new MyEvent())))
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
            .WithEndpoint<Collector>()
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.FailedMessages.IsEmpty, Is.True);
            Assert.That(context.Subscriber1ProcessedConfirmed, Is.True);
            Assert.That(context.Subscriber2ProcessedConfirmed, Is.True);
        }
    }

    public class Context : ScenarioContext
    {
        public bool Subscriber1Subscribed { get; set; }
        public bool Subscriber2Subscribed { get; set; }
        public bool Subscriber1ProcessedConfirmed { get; set; }
        public bool Subscriber2ProcessedConfirmed { get; set; }

        public void MaybeCompleted() => MarkAsCompleted(Subscriber1ProcessedConfirmed, Subscriber2ProcessedConfirmed);
    }

    public class Publisher : EndpointConfigurationBuilder
    {
        public Publisher() => EndpointSetup<DefaultPublisher>(c =>
        {
            c.OnEndpointSubscribed<Context>((s, ctx) =>
            {
                var subscriber1 = Conventions.EndpointNamingConvention(typeof(Subscriber1));
                if (s.SubscriberEndpoint.Contains(subscriber1))
                {
                    ctx.Subscriber1Subscribed = true;
                    ctx.AddTrace($"{subscriber1} is now subscribed");
                }

                var subscriber2 = Conventions.EndpointNamingConvention(typeof(Subscriber2));
                if (s.SubscriberEndpoint.Contains(subscriber2))
                {
                    ctx.Subscriber2Subscribed = true;
                    ctx.AddTrace($"{subscriber2} is now subscribed");
                }
            });
        }, metadata => metadata.RegisterSelfAsPublisherFor<MyEvent>(this));
    }

    public class Subscriber1 : EndpointConfigurationBuilder
    {
        public Subscriber1() => EndpointSetup<DefaultServer>(c =>
        {
            c.DisableFeature<AutoSubscribe>();
            c.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;
            c.EnableOutbox();
            c.ConfigureRouting().RouteToEndpoint(typeof(Subscriber1Processed), Conventions.EndpointNamingConvention(typeof(Collector)));
        }, metadata => metadata.RegisterPublisherFor<MyEvent>(typeof(Publisher)));

        [Handler]
        public class MyHandler : IHandleMessages<MyEvent>
        {
            public Task Handle(MyEvent message, IMessageHandlerContext context) => context.Send(new Subscriber1Processed());
        }
    }

    public class Subscriber2 : EndpointConfigurationBuilder
    {
        public Subscriber2() => EndpointSetup<DefaultServer>(c =>
        {
            c.DisableFeature<AutoSubscribe>();
            c.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;
            c.EnableOutbox();
            c.ConfigureRouting().RouteToEndpoint(typeof(Subscriber2Processed), Conventions.EndpointNamingConvention(typeof(Collector)));
        }, metadata => metadata.RegisterPublisherFor<MyEvent>(typeof(Publisher)));

        [Handler]
        public class MyEventMessageHandler : IHandleMessages<MyEvent>
        {
            public Task Handle(MyEvent message, IMessageHandlerContext context) => context.Send(new Subscriber2Processed());
        }
    }

    public class Collector : EndpointConfigurationBuilder
    {
        public Collector() => EndpointSetup<DefaultServer>();

        [Handler]
        public class Subscriber1ProcessedHandler(Context testContext) : IHandleMessages<Subscriber1Processed>
        {
            public Task Handle(Subscriber1Processed message, IMessageHandlerContext context)
            {
                testContext.Subscriber1ProcessedConfirmed = true;
                testContext.MaybeCompleted();
                return Task.CompletedTask;
            }
        }

        [Handler]
        public class Subscriber2ProcessedHandler(Context testContext) : IHandleMessages<Subscriber2Processed>
        {
            public Task Handle(Subscriber2Processed message, IMessageHandlerContext context)
            {
                testContext.Subscriber2ProcessedConfirmed = true;
                testContext.MaybeCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class MyEvent : IEvent;
    public class Subscriber1Processed : IMessage;
    public class Subscriber2Processed : IMessage;
}
