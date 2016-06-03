namespace NServiceBus.AcceptanceTests.Routing.AutomaticSubscriptions
{
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
                .Done(c => c.EventsSubscribedTo.Count >= 2)
                .Run();

            Assert.True(context.EventsSubscribedTo.Contains(typeof(MyEvent)), "Events only handled by sagas should be auto subscribed");
            Assert.True(context.EventsSubscribedTo.Contains(typeof(MyEventBase)), "Sagas should be auto subscribed even when handling a base class event");
        }

        class Context : ScenarioContext
        {
            public Context()
            {
                EventsSubscribedTo = new List<Type>();
            }

            public List<Type> EventsSubscribedTo { get; }
        }

        class Subscriber : EndpointConfigurationBuilder
        {
            public Subscriber()
            {
                EndpointSetup<DefaultServer>(c => c.Pipeline.Register("SubscriptionSpy", typeof(SubscriptionSpy), "Spies on subscriptions made"))
                    .AddMapping<MyEventWithParent>(typeof(Subscriber)) //just map to our self for this test
                    .AddMapping<MyEvent>(typeof(Subscriber)); //just map to our self for this test
            }

            public class SubscriptionSpy : Behavior<ISubscribeContext>
            {
                public SubscriptionSpy(Context testContext)
                {
                    this.testContext = testContext;
                }

                public override async Task Invoke(ISubscribeContext context, Func<Task> next)
                {
                    await next().ConfigureAwait(false);

                    testContext.EventsSubscribedTo.Add(context.EventType);
                }

                Context testContext;
            }

            public class AutoSubscriptionSaga : Saga<AutoSubscriptionSaga.AutoSubscriptionSagaData>, IAmStartedByMessages<MyEvent>
            {
                public Task Handle(MyEvent message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<AutoSubscriptionSagaData> mapper)
                {
                    mapper.ConfigureMapping<MyEvent>(msg => msg.SomeId).ToSaga(saga => saga.SomeId);
                }

                public class AutoSubscriptionSagaData : ContainSagaData
                {
                    public virtual string SomeId { get; set; }
                }
            }

            public class MySagaThatReactsOnASuperClassEvent : Saga<MySagaThatReactsOnASuperClassEvent.SuperClassEventSagaData>,
                IAmStartedByMessages<MyEventBase>
            {
                public Task Handle(MyEventBase message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SuperClassEventSagaData> mapper)
                {
                    mapper.ConfigureMapping<MyEventBase>(msg => msg.SomeId).ToSaga(saga => saga.SomeId);
                }

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

        public class MyEventWithParent : MyEventBase
        {
        }

        public class MyMessage : IMessage
        {
        }

        public class MyEvent : IEvent
        {
            public string SomeId { get; set; }
        }
    }
}