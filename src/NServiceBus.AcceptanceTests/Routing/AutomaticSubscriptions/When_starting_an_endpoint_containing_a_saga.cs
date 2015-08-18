namespace NServiceBus.AcceptanceTests.Routing.AutomaticSubscriptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Saga;
    using NUnit.Framework;

    [TestFixture]
    public class When_starting_an_endpoint_containing_a_saga : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_autoSubscribe_the_saga_messageHandler_by_default()
        {
            var context = Scenario.Define<Context>()
                   .WithEndpoint<Subscriber>()
                   .Done(c => c.EventsSubscribedTo.Count >= 2)
                   .Run();


            Assert.True(context.EventsSubscribedTo.Contains(typeof(MyEvent)), "Events only handled by sagas should be auto subscribed");
            Assert.True(context.EventsSubscribedTo.Contains(typeof(MyEventBase)), "Sagas should be auto subscribed even when handling a base class event");
        }

        [Test]
        public void Should_not_autoSubscribe_messages_handled_by_sagas_if_asked_to()
        {
            var context = Scenario.Define<Context>()
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

                public override void Invoke(SubscribeContext context, Action next)
                {
                    next();

                    testContext.EventsSubscribedTo.Add(context.EventType);
                }
            }

            public class AutoSubscriptionSaga : Saga<AutoSubscriptionSaga.AutoSubscriptionSagaData>, IAmStartedByMessages<MyEvent>
            {
                public void Handle(MyEvent message)
                {
                }

                public class AutoSubscriptionSagaData : ContainSagaData
                {
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<AutoSubscriptionSagaData> mapper)
                {
                }
            }

            public class MySagaThatReactsOnASuperClassEvent : Saga<MySagaThatReactsOnASuperClassEvent.SuperClassEventSagaData>, 
                IAmStartedByMessages<MyEventBase>
            {
                public void Handle(MyEventBase message)
                {
                }


                public class SuperClassEventSagaData : ContainSagaData
                {
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SuperClassEventSagaData> mapper)
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