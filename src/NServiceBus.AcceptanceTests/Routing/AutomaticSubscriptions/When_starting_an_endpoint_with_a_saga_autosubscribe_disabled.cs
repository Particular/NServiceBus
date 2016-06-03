namespace NServiceBus.AcceptanceTests.Routing.AutomaticSubscriptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
                .WithEndpoint<Subscriber>(g => g.CustomConfig(c => c.AutoSubscribe().DoNotAutoSubscribeSagas()))
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.False(context.EventsSubscribedTo.Any(), "Events only handled by sagas should not be auto subscribed");
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

            public class NotAutoSubscribedSaga : Saga<NotAutoSubscribedSaga.NotAutoSubscribedSagaSagaData>, IAmStartedByMessages<MyEvent>
            {
                public Task Handle(MyEvent message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<NotAutoSubscribedSagaSagaData> mapper)
                {
                    mapper.ConfigureMapping<MyEvent>(msg => msg.SomeId).ToSaga(saga => saga.SomeId);
                }

                public class NotAutoSubscribedSagaSagaData : ContainSagaData
                {
                    public virtual string SomeId { get; set; }
                }
            }

            public class NotAutoSubsubscribedSagaThatReactsOnASuperClassEvent : Saga<NotAutoSubsubscribedSagaThatReactsOnASuperClassEvent.NotAutosubscribeSuperClassEventSagaData>,
                IAmStartedByMessages<MyEventBase>
            {
                public Task Handle(MyEventBase message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<NotAutosubscribeSuperClassEventSagaData> mapper)
                {
                    mapper.ConfigureMapping<MyEventBase>(saga => saga.SomeId).ToSaga(saga => saga.SomeId);
                }

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