namespace NServiceBus.AcceptanceTests.Core.AutomaticSubscriptions
{
    using AcceptanceTests.EndpointTemplates;
    using Logging;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.Pipeline;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    [TestFixture]
    public class When_excluding_event_type_from_autosubscribe : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_subscribe_excluded_events()
        {
            var ctx = await Scenario.Define<Context>()
                .WithEndpoint<Subscriber>()
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.AreEqual(1, ctx.EventsSubscribedTo.Count);
            Assert.AreEqual(typeof(EventToSubscribeTo), ctx.EventsSubscribedTo[0]);

            CollectionAssert.IsEmpty(ctx.Logs.Where(l => l.Level == LogLevel.Error));
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
                EndpointSetup<DefaultServer>(c =>
                {
                    c.Pipeline.Register("SubscriptionSpy", new SubscriptionSpy((Context)ScenarioContext), "Spies on subscriptions made");
                    c.AutoSubscribe().DisableFor<EventToExclude>();
                    c.AutoSubscribe().DisableFor(typeof(EventWithNoPublisher));
                },
                    metadata =>
                    {
                        metadata.RegisterPublisherFor<EventToSubscribeTo>(typeof(Subscriber));
                        metadata.RegisterPublisherFor<EventToExclude>(typeof(Subscriber));
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

                    testContext.EventsSubscribedTo.Add(context.EventType);
                }

                Context testContext;
            }

            class MyMessageHandler : IHandleMessages<EventToSubscribeTo>
            {
                public Task Handle(EventToSubscribeTo message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }
            }

            public class EventMessageHandler : IHandleMessages<EventToExclude>
            {
                public Task Handle(EventToExclude message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }
            }

            public class MyEventWithNoRoutingHandler : IHandleMessages<EventWithNoPublisher>
            {
                public Task Handle(EventWithNoPublisher message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }
            }
        }

        public class EventToSubscribeTo : IEvent { }
        public class EventToExclude : IEvent { }
        public class EventWithNoPublisher : IEvent { }
    }
}
