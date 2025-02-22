﻿namespace NServiceBus.AcceptanceTests.Routing.NativePublishSubscribe;

using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NServiceBus;
using NUnit.Framework;

public class When_publishing_from_sendonly : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_be_delivered_to_all_subscribers()
    {
        Requires.NativePubSubSupport();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<SendOnlyPublisher>(b => b.When((session, c) => session.Publish(new MyEvent())))
            .WithEndpoint<Subscriber>()
            .Done(c => c.SubscriberGotTheEvent)
            .Run();

        Assert.That(context.SubscriberGotTheEvent, Is.True);
    }

    public class Context : ScenarioContext
    {
        public bool SubscriberGotTheEvent { get; set; }
    }

    public class SendOnlyPublisher : EndpointConfigurationBuilder
    {
        public SendOnlyPublisher()
        {
            EndpointSetup<DefaultPublisher>(b =>
            {
                b.SendOnly();
            }, metadata => metadata.RegisterSelfAsPublisherFor<MyEvent>(this));
        }
    }

    public class Subscriber : EndpointConfigurationBuilder
    {
        public Subscriber()
        {
            EndpointSetup<DefaultServer>(_ => { }, metadata => metadata.RegisterPublisherFor<MyEvent, SendOnlyPublisher>());
        }

        public class MyHandler : IHandleMessages<MyEvent>
        {
            public MyHandler(Context context)
            {
                testContext = context;
            }

            public Task Handle(MyEvent messageThatIsEnlisted, IMessageHandlerContext context)
            {
                testContext.SubscriberGotTheEvent = true;

                return Task.CompletedTask;
            }

            Context testContext;
        }
    }

    public class MyEvent : IEvent
    {
    }
}