namespace NServiceBus.AcceptanceTests.Routing.MessageDrivenSubscriptions;

using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class Pub_to_scaled_out_subs : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Each_event_should_be_delivered_to_single_instance_of_each_subscriber()
    {
        Requires.MessageDrivenPubSub();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<Publisher>(b => b.When(c => c.SubscribersCounter == 4, async (session, c) => { await session.Publish(new MyEvent()); }))
            .WithEndpoint<SubscriberA>(b => b.CustomConfig(c => c.MakeInstanceUniquelyAddressable("1")))
            .WithEndpoint<SubscriberA>(b => b.CustomConfig(c => c.MakeInstanceUniquelyAddressable("2")))
            .WithEndpoint<SubscriberB>(b => b.CustomConfig(c => c.MakeInstanceUniquelyAddressable("1")))
            .WithEndpoint<SubscriberB>(b => b.CustomConfig(c => c.MakeInstanceUniquelyAddressable("2")))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.ProcessedByA, Is.EqualTo(1));
            Assert.That(context.ProcessedByB, Is.EqualTo(1));
        }

    }

    public class Context : ScenarioContext
    {
        public int SubscribersCounter => subscribersCounter;

        public int ProcessedByA => processedByA;

        public int ProcessedByB => processedByB;

        public void IncrementA()
        {
            Interlocked.Increment(ref processedByA);
            MaybeCompleted();
        }

        public void IncrementB()
        {
            Interlocked.Increment(ref processedByB);
            MaybeCompleted();
        }

        public void IncrementSubscribersCounter() => Interlocked.Increment(ref subscribersCounter);

        void MaybeCompleted() => MarkAsCompleted(ProcessedByA > 0, ProcessedByB > 0);

        int processedByA;
        int processedByB;
        int subscribersCounter;
    }

    public class Publisher : EndpointConfigurationBuilder
    {
        public Publisher() =>
            EndpointSetup<DefaultServer>(c =>
            {
                c.OnEndpointSubscribed<Context>((s, context) => { context.IncrementSubscribersCounter(); });
            }, metadata => metadata.RegisterSelfAsPublisherFor<MyEvent>(this));
    }

    public class SubscriberA : EndpointConfigurationBuilder
    {
        public SubscriberA() => EndpointSetup<DefaultServer>(publisherMetadata: metadata => metadata.RegisterPublisherFor<MyEvent, Publisher>());

        public class MyHandler(Context testContext) : IHandleMessages<MyEvent>
        {
            public Task Handle(MyEvent message, IMessageHandlerContext context)
            {
                testContext.IncrementA();
                return Task.CompletedTask;
            }
        }
    }

    public class SubscriberB : EndpointConfigurationBuilder
    {
        public SubscriberB() => EndpointSetup<DefaultServer>(publisherMetadata: metadata => metadata.RegisterPublisherFor<MyEvent, Publisher>());

        public class MyHandler(Context testContext) : IHandleMessages<MyEvent>
        {
            public Task Handle(MyEvent message, IMessageHandlerContext context)
            {
                testContext.IncrementB();
                return Task.CompletedTask;
            }
        }
    }

    public class MyEvent : IEvent;
}