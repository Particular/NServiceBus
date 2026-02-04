namespace NServiceBus.AcceptanceTests.Routing.NativePublishSubscribe;

using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class MultiSubscribeToPolymorphicEvent : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Both_events_should_be_delivered()
    {
        Requires.NativePubSubSupport();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<Publisher1>(b => b.When((session, c) =>
            {
                c.AddTrace("Publishing MyEvent1");
                return session.Publish(new MyEvent1());
            }))
            .WithEndpoint<Publisher2>(b => b.When((session, c) =>
            {
                c.AddTrace("Publishing MyEvent2");
                return session.Publish(new MyEvent2());
            }))
            .WithEndpoint<Subscriber>()
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.SubscriberGotIMyEvent, Is.True);
            Assert.That(context.SubscriberGotMyEvent2, Is.True);
        }
    }

    public class Context : ScenarioContext
    {
        public bool SubscriberGotIMyEvent { get; set; }
        public bool SubscriberGotMyEvent2 { get; set; }

        public void MaybeCompleted() => MarkAsCompleted(SubscriberGotIMyEvent, SubscriberGotMyEvent2);
    }

    public class Publisher1 : EndpointConfigurationBuilder
    {
        public Publisher1() => EndpointSetup<DefaultPublisher>(_ => { }, metadata => metadata.RegisterSelfAsPublisherFor<MyEvent1>(this));
    }

    public class Publisher2 : EndpointConfigurationBuilder
    {
        public Publisher2() => EndpointSetup<DefaultPublisher>(_ => { }, metadata => metadata.RegisterSelfAsPublisherFor<MyEvent2>(this));
    }

    public class Subscriber : EndpointConfigurationBuilder
    {
        public Subscriber() =>
            EndpointSetup<DefaultServer>(_ => { }, metadata =>
            {
                metadata.RegisterPublisherFor<IMyEvent, Publisher1>();
                metadata.RegisterPublisherFor<IMyEvent, Publisher2>();
            });

        [Handler]
        public class MyHandler(Context testContext) : IHandleMessages<IMyEvent>
        {
            public Task Handle(IMyEvent messageThatIsEnlisted, IMessageHandlerContext context)
            {
                testContext.AddTrace($"Got event '{messageThatIsEnlisted}'");
                if (messageThatIsEnlisted is MyEvent2)
                {
                    testContext.SubscriberGotMyEvent2 = true;
                }
                else
                {
                    testContext.SubscriberGotIMyEvent = true;
                }

                testContext.MaybeCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class MyEvent1 : IMyEvent;

    public class MyEvent2 : IMyEvent;

    public interface IMyEvent : IEvent;
}