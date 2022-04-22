namespace NServiceBus.AcceptanceTests.Core.AutomaticSubscriptions
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    public class When_starting_an_endpoint_with_autosubscribe_disabled : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_autosubscribe_any_events()
        {

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Subscriber>()
                .Done(ctx => ctx.EndpointsStarted)
                .Run();

            Assert.IsEmpty(context.SubscribedEvents);
        }

        class Context : ScenarioContext
        {
            public ConcurrentBag<Type> SubscribedEvents { get; set; } = new ConcurrentBag<Type>();
        }

        class Subscriber : EndpointFromTemplate<DefaultServer>
        {
            public Subscriber()
            {
                EndpointSetup(c =>
                {
                    c.DisableFeature<AutoSubscribe>();
                    c.Pipeline.Register(typeof(SubscribeSpy), "Inspects all subscribe operations");
                });
            }

            class SubscribeSpy : Behavior<ISubscribeContext>
            {
                readonly Context testContext;

                public SubscribeSpy(Context testContext) => this.testContext = testContext;

                public override Task Invoke(ISubscribeContext context, Func<Task> next)
                {
                    foreach (var eventType in context.EventTypes)
                    {
                        testContext.SubscribedEvents.Add(eventType);
                    }

                    return next();
                }
            }

            class EventHandler : IHandleMessages<NonSubscribedEvent>
            {
                public Task Handle(NonSubscribedEvent message, IMessageHandlerContext context) => throw new InvalidOperationException();
            }
        }

        public class NonSubscribedEvent : IEvent
        {
        }
    }
}