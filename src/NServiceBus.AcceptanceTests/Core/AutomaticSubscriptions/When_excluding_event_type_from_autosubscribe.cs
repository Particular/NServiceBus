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

        Assert.That(ctx.EventsSubscribedTo, Has.Count.EqualTo(1));
        Assert.That(ctx.EventsSubscribedTo[0], Is.EqualTo(typeof(EventToSubscribeTo)));

        Assert.That(ctx.Logs.Where(l => l.LoggerName == typeof(AutoSubscribe).FullName && l.Level == LogLevel.Error), Is.Empty);
    }

    public class Context : ScenarioContext
    {
        public List<Type> EventsSubscribedTo { get; } = [];
    }

    public class Subscriber : EndpointConfigurationBuilder
    {
        public Subscriber() =>
            EndpointSetup<DefaultServer>((c, r) =>
                {
                    c.Pipeline.Register("SubscriptionSpy", new SubscriptionSpy((Context)r.ScenarioContext), "Spies on subscriptions made");
                    c.AutoSubscribe().DisableFor<EventToExclude>();
                    c.AutoSubscribe().DisableFor<EventWithNoPublisher>();
                },
                metadata =>
                {
                    metadata.RegisterPublisherFor<EventToSubscribeTo, Subscriber>();
                    metadata.RegisterPublisherFor<EventToExclude, Subscriber>();
                });

        class SubscriptionSpy(Context testContext) : IBehavior<ISubscribeContext, ISubscribeContext>
        {
            public async Task Invoke(ISubscribeContext context, Func<ISubscribeContext, Task> next)
            {
                await next(context).ConfigureAwait(false);

                testContext.EventsSubscribedTo.AddRange(context.EventTypes);
            }
        }

        [Handler]
        public class MyMessageHandler : IHandleMessages<EventToSubscribeTo>
        {
            public Task Handle(EventToSubscribeTo message, IMessageHandlerContext context) => Task.CompletedTask;
        }

        [Handler]
        public class EventMessageHandler : IHandleMessages<EventToExclude>
        {
            public Task Handle(EventToExclude message, IMessageHandlerContext context) => Task.CompletedTask;
        }

        [Handler]
        public class MyEventWithNoRoutingHandler : IHandleMessages<EventWithNoPublisher>
        {
            public Task Handle(EventWithNoPublisher message, IMessageHandlerContext context) => Task.CompletedTask;
        }
    }

    public class EventToSubscribeTo : IEvent;
    public class EventToExclude : IEvent;
    public class EventWithNoPublisher : IEvent;
}