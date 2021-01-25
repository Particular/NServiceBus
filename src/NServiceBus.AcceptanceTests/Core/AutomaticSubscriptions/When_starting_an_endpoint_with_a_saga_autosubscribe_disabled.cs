namespace NServiceBus.AcceptanceTests.Core.AutomaticSubscriptions
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
                .WithEndpoint<Subscriber>()
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
                EndpointSetup<DefaultServer>((c, r) =>
                    {
                        c.Pipeline.Register("SubscriptionSpy", new SubscriptionSpy((Context)r.ScenarioContext), "Spies on subscriptions made");
                        c.AutoSubscribe().DoNotAutoSubscribeSagas();
                    },
                    metadata =>
                    {
                        metadata.RegisterPublisherFor<MyEventWithParent>(typeof(Subscriber));
                        metadata.RegisterPublisherFor<MyEvent>(typeof(Subscriber));
                    });
            }

            class SubscriptionSpy : IBehavior<ISubscribeContext, ISubscribeContext>
            {
                public SubscriptionSpy(Context testContext)
                {
                    this.testContext = testContext;
                }

                public async Task Invoke(ISubscribeContext context, Func<ISubscribeContext, Task> next)
                {
                    await next(context).ConfigureAwait(false);

                    testContext.EventsSubscribedTo.AddRange(context.EventTypes);
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

            public class NotAutoSubscribedSagaThatReactsOnASuperClassEvent : Saga<NotAutoSubscribedSagaThatReactsOnASuperClassEvent.NotAutosubscribeSuperClassEventSagaData>,
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