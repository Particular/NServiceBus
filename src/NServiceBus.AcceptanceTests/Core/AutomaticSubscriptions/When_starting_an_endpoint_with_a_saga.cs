namespace NServiceBus.AcceptanceTests.Core.AutomaticSubscriptions;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NServiceBus.Pipeline;
using NUnit.Framework;

[TestFixture]
public class When_starting_an_endpoint_with_a_saga : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_autoSubscribe_the_saga_messageHandler_by_default()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Subscriber>()
            .Run();

        Assert.That(context.EventsSubscribedTo, Does.Contain(typeof(MyEvent)), "Events only handled by sagas should be auto subscribed");
        Assert.That(context.EventsSubscribedTo, Does.Contain(typeof(MyEventBase)), "Sagas should be auto subscribed even when handling a base class event");
    }

    class Context : ScenarioContext
    {
        public List<Type> EventsSubscribedTo { get; } = [];

        public void MaybeCompleted() => MarkAsCompleted(EventsSubscribedTo.Count >= 2);
    }

    class Subscriber : EndpointConfigurationBuilder
    {
        public Subscriber() =>
            EndpointSetup<DefaultServer>((c, r) => c.Pipeline.Register("SubscriptionSpy", new SubscriptionSpy((Context)r.ScenarioContext), "Spies on subscriptions made"),
                metadata =>
                {
                    metadata.RegisterPublisherFor<MyEventBase, Subscriber>();
                    metadata.RegisterPublisherFor<MyEvent, Subscriber>();
                });

        class SubscriptionSpy(Context testContext) : IBehavior<ISubscribeContext, ISubscribeContext>
        {
            public async Task Invoke(ISubscribeContext context, Func<ISubscribeContext, Task> next)
            {
                await next(context).ConfigureAwait(false);

                testContext.EventsSubscribedTo.AddRange(context.EventTypes);
                testContext.MaybeCompleted();
            }
        }

        public class AutoSubscriptionSaga : Saga<AutoSubscriptionSaga.AutoSubscriptionSagaData>, IAmStartedByMessages<MyEvent>
        {
            public Task Handle(MyEvent message, IMessageHandlerContext context) => Task.CompletedTask;

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<AutoSubscriptionSagaData> mapper) =>
                mapper.MapSaga(s => s.SomeId)
                    .ToMessage<MyEvent>(msg => msg.SomeId);

            public class AutoSubscriptionSagaData : ContainSagaData
            {
                public virtual string SomeId { get; set; }
            }
        }

        public class MySagaThatReactsOnASuperClassEvent : Saga<MySagaThatReactsOnASuperClassEvent.SuperClassEventSagaData>,
            IAmStartedByMessages<MyEventBase>
        {
            public Task Handle(MyEventBase message, IMessageHandlerContext context) => Task.CompletedTask;

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SuperClassEventSagaData> mapper) =>
                mapper.MapSaga(s => s.SomeId)
                    .ToMessage<MyEventBase>(msg => msg.SomeId);

            public class SuperClassEventSagaData : ContainSagaData
            {
                public virtual string SomeId { get; set; }
            }
        }
    }

    public class MyEventBase : IEvent
    {
        public string SomeId { get; set; }
    }


    public class MyEvent : IEvent
    {
        public string SomeId { get; set; }
    }
}