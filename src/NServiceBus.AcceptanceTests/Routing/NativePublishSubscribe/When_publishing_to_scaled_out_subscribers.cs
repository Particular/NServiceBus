namespace NServiceBus.AcceptanceTests.Routing.NativePublishSubscribe;

using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_publishing_to_scaled_out_subscribers : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_route_event_to_shared_queue()
    {
        Requires.NativePubSubSupport();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<Publisher>(b => b
                .When(session => session.Publish(new MyEvent())))
            .WithEndpoint<SubscriberA>(b => b.CustomConfig(c => c.MakeInstanceUniquelyAddressable("1")))
            .WithEndpoint<SubscriberA>(b => b.CustomConfig(c => c.MakeInstanceUniquelyAddressable("2")))
            .WithEndpoint<SubscriberB>(b => b.CustomConfig(c => c.MakeInstanceUniquelyAddressable("1")))
            .WithEndpoint<SubscriberB>(b => b.CustomConfig(c => c.MakeInstanceUniquelyAddressable("2")))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.SubscriberACounter, Is.EqualTo(1));
            Assert.That(context.SubscriberBCounter, Is.EqualTo(1));
        }
    }

    public class Context : ScenarioContext
    {
        public int SubscriberACounter => subscriberACounter;

        public int SubscriberBCounter => subscriberBCounter;

        public void IncrementSubscriberACounter()
        {
            Interlocked.Increment(ref subscriberACounter);
            MaybeCompleted();
        }

        public void IncrementSubscriberBCounter()
        {
            Interlocked.Increment(ref subscriberBCounter);
            MaybeCompleted();
        }

        void MaybeCompleted() => MarkAsCompleted(SubscriberACounter > 0, SubscriberBCounter > 0);

        int subscriberACounter;
        int subscriberBCounter;
    }

    public class Publisher : EndpointConfigurationBuilder
    {
        public Publisher() => EndpointSetup<DefaultServer>(_ => { }, metadata => metadata.RegisterSelfAsPublisherFor<MyEvent>(this));
    }

    public class SubscriberA : EndpointConfigurationBuilder
    {
        public SubscriberA() => EndpointSetup<DefaultServer>(_ => { }, metadata => metadata.RegisterPublisherFor<MyEvent, Publisher>());

        public class MyHandler(Context testContext) : IHandleMessages<MyEvent>
        {
            public Task Handle(MyEvent message, IMessageHandlerContext context)
            {
                testContext.IncrementSubscriberACounter();
                return Task.CompletedTask;
            }
        }
    }

    public class SubscriberB : EndpointConfigurationBuilder
    {
        public SubscriberB() => EndpointSetup<DefaultServer>(_ => { }, metadata => metadata.RegisterPublisherFor<MyEvent, Publisher>());

        public class MyHandler(Context testContext) : IHandleMessages<MyEvent>
        {
            public Task Handle(MyEvent message, IMessageHandlerContext context)
            {
                testContext.IncrementSubscriberBCounter();
                return Task.CompletedTask;
            }
        }
    }

    public class MyEvent : IEvent;
}