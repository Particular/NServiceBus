namespace NServiceBus.AcceptanceTests.Routing.NativePublishSubscribe;

using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Features;
using NUnit.Framework;

public class When_subscribing_to_a_base_event : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Both_base_and_specific_events_should_be_delivered()
    {
        Requires.NativePubSubSupport();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<Publisher>(b => b.When(c => c.SubscriberSubscribed, async session =>
            {
                await session.Publish(new SpecificEvent());
                await session.Publish<IBaseEvent>();
            }))
            .WithEndpoint<GeneralSubscriber>(b => b.When(async (session, c) =>
            {
                await session.Subscribe<IBaseEvent>();
                c.SubscriberSubscribed = true;
            }))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.SubscriberGotBaseEvent, Is.True);
            Assert.That(context.SubscriberGotSpecificEvent, Is.True);
        }
    }

    public class Context : ScenarioContext
    {
        public bool SubscriberGotBaseEvent { get; set; }
        public bool SubscriberGotSpecificEvent { get; set; }
        public bool SubscriberSubscribed { get; set; }

        public void MaybeCompleted() => MarkAsCompleted(SubscriberGotBaseEvent, SubscriberGotSpecificEvent);
    }

    public class Publisher : EndpointConfigurationBuilder
    {
        public Publisher() =>
            EndpointSetup<DefaultPublisher>(_ => { }, metadata =>
            {
                metadata.RegisterSelfAsPublisherFor<SpecificEvent>(this);
                metadata.RegisterSelfAsPublisherFor<IBaseEvent>(this);
            });
    }

    public class GeneralSubscriber : EndpointConfigurationBuilder
    {
        public GeneralSubscriber() =>
            EndpointSetup<DefaultServer>(c => { c.DisableFeature<AutoSubscribe>(); },
                metadata => metadata.RegisterPublisherFor<IBaseEvent, Publisher>());

        public class MyHandler(Context testContext) : IHandleMessages<IBaseEvent>
        {
            public Task Handle(IBaseEvent messageThatIsEnlisted, IMessageHandlerContext context)
            {
                if (messageThatIsEnlisted is SpecificEvent)
                {
                    testContext.SubscriberGotSpecificEvent = true;
                }
                else
                {
                    testContext.SubscriberGotBaseEvent = true;
                }
                testContext.MaybeCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class SpecificEvent : IBaseEvent;

    public interface IBaseEvent : IEvent;
}