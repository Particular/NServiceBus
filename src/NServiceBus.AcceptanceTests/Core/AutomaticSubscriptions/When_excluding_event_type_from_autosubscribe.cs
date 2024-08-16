namespace NServiceBus.AcceptanceTests.Core.AutomaticSubscriptions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Features;
using Logging;
using NServiceBus.Pipeline;
using NUnit.Framework;

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

        Assert.That(ctx.EventsSubscribedTo.Count, Is.EqualTo(1));
        Assert.That(ctx.EventsSubscribedTo[0], Is.EqualTo(typeof(EventToSubscribeTo)));

        CollectionAssert.IsEmpty(ctx.Logs.Where(l => l.LoggerName == typeof(AutoSubscribe).FullName && l.Level == LogLevel.Error));
    }

    class Context : ScenarioContext
    {
        public Context()
        {
            EventsSubscribedTo = [];
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

                testContext.EventsSubscribedTo.AddRange(context.EventTypes);
            }

            Context testContext;
        }

        class MyMessageHandler : IHandleMessages<EventToSubscribeTo>
        {
            public Task Handle(EventToSubscribeTo message, IMessageHandlerContext context)
            {
                return Task.CompletedTask;
            }
        }

        public class EventMessageHandler : IHandleMessages<EventToExclude>
        {
            public Task Handle(EventToExclude message, IMessageHandlerContext context)
            {
                return Task.CompletedTask;
            }
        }

        public class MyEventWithNoRoutingHandler : IHandleMessages<EventWithNoPublisher>
        {
            public Task Handle(EventWithNoPublisher message, IMessageHandlerContext context)
            {
                return Task.CompletedTask;
            }
        }
    }

    public class EventToSubscribeTo : IEvent { }
    public class EventToExclude : IEvent { }
    public class EventWithNoPublisher : IEvent { }
}