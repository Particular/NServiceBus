namespace NServiceBus.AcceptanceTests.Core.AutomaticSubscriptions;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NServiceBus.Pipeline;
using NUnit.Framework;

[TestFixture]
public class When_starting_an_endpoint_with_a_saga_autosubscribe_disabled : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_not_autoSubscribe_messages_handled_by_sagas_if_asked_to()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Subscriber>()
            .Done(c => c.EndpointsStarted)
            .Run();

        Assert.That(context.EventsSubscribedTo.Count, Is.EqualTo(0), "Events only handled by sagas should not be auto subscribed");
    }

    class Context : ScenarioContext
    {
        public List<Type> EventsSubscribedTo { get; } = [];
    }

    class Subscriber : EndpointConfigurationBuilder
    {
        public Subscriber() =>
            EndpointSetup<DefaultServer>((c, r) =>
                {
                    c.Pipeline.Register("SubscriptionSpy", new SubscriptionSpy((Context)r.ScenarioContext), "Spies on subscriptions made");
                    c.AutoSubscribe().DoNotAutoSubscribeSagas();
                },
                metadata =>
                {
                    metadata.RegisterPublisherFor<MyEventWithParent, Subscriber>();
                    metadata.RegisterPublisherFor<MyEvent, Subscriber>();
                });

        class SubscriptionSpy(Context testContext) : IBehavior<ISubscribeContext, ISubscribeContext>
        {
            public async Task Invoke(ISubscribeContext context, Func<ISubscribeContext, Task> next)
            {
                await next(context).ConfigureAwait(false);

                testContext.EventsSubscribedTo.AddRange(context.EventTypes);
            }
        }

        public class NotAutoSubscribedSaga : Saga<NotAutoSubscribedSaga.NotAutoSubscribedSagaSagaData>, IAmStartedByMessages<MyEvent>
        {
            public Task Handle(MyEvent message, IMessageHandlerContext context) => Task.CompletedTask;

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<NotAutoSubscribedSagaSagaData> mapper) =>
                mapper.MapSaga(s => s.SomeId)
                    .ToMessage<MyEvent>(msg => msg.SomeId);

            public class NotAutoSubscribedSagaSagaData : ContainSagaData
            {
                public virtual string SomeId { get; set; }
            }
        }

        public class NotAutoSubscribedSagaThatReactsOnASuperClassEvent : Saga<NotAutoSubscribedSagaThatReactsOnASuperClassEvent.NotAutosubscribeSuperClassEventSagaData>,
            IAmStartedByMessages<MyEventBase>
        {
            public Task Handle(MyEventBase message, IMessageHandlerContext context) => Task.CompletedTask;

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<NotAutosubscribeSuperClassEventSagaData> mapper) =>
                mapper.MapSaga(s => s.SomeId)
                    .ToMessage<MyEventBase>(msg => msg.SomeId);

            public class NotAutosubscribeSuperClassEventSagaData : ContainSagaData
            {
                public virtual string SomeId { get; set; }
            }
        }
    }

    public class MyEventBase : IEvent
    {
        public string SomeId { get; set; }
    }

    public class MyEventWithParent : MyEventBase;

    public class MyEvent : IEvent
    {
        public string SomeId { get; set; }
    }
}