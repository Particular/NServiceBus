﻿namespace NServiceBus.AcceptanceTests.Core.Routing.MessageDrivenSubscriptions;

using System.Threading.Tasks;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Customization;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Features;
using NServiceBus.Routing.MessageDrivenSubscriptions;
using NUnit.Framework;

public class Extend_event_routing : NServiceBusAcceptanceTest
{
    static string PublisherEndpoint => Conventions.EndpointNamingConvention(typeof(Publisher));

    [Test]
    public async Task Should_route_events_correctly()
    {
        Requires.MessageDrivenPubSub();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<Publisher>(b =>
                b.When(c => c.Subscribed, session => session.Publish<MyEvent>())
            )
            .WithEndpoint<Subscriber>(b => b.When(async (session, c) => { await session.Subscribe<MyEvent>(); }))
            .Done(c => c.MessageDelivered)
            .Run();

        Assert.That(context.MessageDelivered, Is.True);
    }

    public class Context : ScenarioContext
    {
        public bool MessageDelivered { get; set; }
        public bool Subscribed { get; set; }
    }

    public class Publisher : EndpointConfigurationBuilder
    {
        public Publisher()
        {
            EndpointSetup<DefaultPublisher>(b => b.OnEndpointSubscribed<Context>((s, context) => { context.Subscribed = true; }));
        }
    }

    public class Subscriber : EndpointConfigurationBuilder
    {
        public Subscriber()
        {
            EndpointSetup<DefaultServer>(c =>
            {
                c.DisableFeature<AutoSubscribe>();
                c.GetSettings().GetOrCreate<Publishers>()
                    .AddOrReplacePublishers("CustomRoutingFeature",
                    [
                        new PublisherTableEntry(typeof(MyEvent), PublisherAddress.CreateFromEndpointName(PublisherEndpoint))
                    ]);
            });
        }

        public class MyHandler : IHandleMessages<MyEvent>
        {
            public MyHandler(Context context)
            {
                testContext = context;
            }

            public Task Handle(MyEvent evnt, IMessageHandlerContext context)
            {
                testContext.MessageDelivered = true;
                return Task.CompletedTask;
            }

            Context testContext;
        }
    }

    public class MyEvent : IEvent
    {
    }
}