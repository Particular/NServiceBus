namespace NServiceBus.AcceptanceTests.Routing.AutomaticSubscriptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
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
            public List<Type> EventsSubscribedTo { get; set; }
        }

        class Subscriber : EndpointConfigurationBuilder
        {
            public Subscriber()
            {
                EndpointSetup<DefaultServer>(c => c.Pipeline.Register("SubscriptionSpy", typeof(SubscriptionSpy), "Spies on subscriptions made"))
                    .AddMapping<MyEventWithParent>(typeof(Subscriber)) //just map to our self for this test
                    .AddMapping<MyEvent>(typeof(Subscriber)); //just map to our self for this test
            }

            public class SubscriptionSpy : Behavior<SubscribeContext>
            {
                Context testContext;

                public SubscriptionSpy(Context testContext)
                {
                    this.testContext = testContext;
                }

                public override async Task Invoke(SubscribeContext context, Func<Task> next)
                {
                    await next().ConfigureAwait(false);

                    testContext.EventsSubscribedTo.Add(context.EventType);
                }
            }

            public class NotAutoSubscribedSaga : Saga<NotAutoSubscribedSaga.NotAutoSubscribedSagaSagaData>, IAmStartedByMessages<MyEvent>
            {
                public Task Handle(MyEvent message)
                {
                    return Task.FromResult(0);
                }

                public class NotAutoSubscribedSagaSagaData : ContainSagaData
                {
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<NotAutoSubscribedSagaSagaData> mapper)
                {
                }
            }

            public class NotAutosubsubscribedSagaThatReactsOnASuperClassEvent : Saga<NotAutosubsubscribedSagaThatReactsOnASuperClassEvent.NotAutosubscribeSuperClassEventSagaData>,
                IAmStartedByMessages<MyEventBase>
            {
                public Task Handle(MyEventBase message)
                {
                    return Task.FromResult(0);
                }

                public class NotAutosubscribeSuperClassEventSagaData : ContainSagaData
                {
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<NotAutosubscribeSuperClassEventSagaData> mapper)
                {
                }
            }

        }


        public class MyEventBase : IEvent { }

        public class MyEventWithParent : MyEventBase { }

        public class MyMessage : IMessage { }

        public class MyEvent : IEvent { }
    }
}