namespace NServiceBus.AcceptanceTests.Core.Routing.AutomaticSubscriptions
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_handling_local_event : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_autosubscribe_to_event()
        {
            var ctx = await Scenario.Define<Context>(x => x.Id = Guid.NewGuid())
                .WithEndpoint<PublisherAndSubscriber>(b => b
                .When((session, context) =>
                {
                    if (context.HasNativePubSubSupport)
                    {
                        context.EventSubscribed = true;
                    }
                    return Task.FromResult(0);
                })
                .When(c => c.EventSubscribed || c.HasNativePubSubSupport, (session, context) => session.Publish(new Event { ContextId = context.Id })))
                .Done(c => c.GotEvent)
                .Run().ConfigureAwait(false);

            Assert.True(ctx.GotEvent);
        }

        public class Context : ScenarioContext
        {
            public Guid Id { get; set; }
            public bool GotEvent { get; set; }
            public bool EventSubscribed { get; set; }
        }

        public class PublisherAndSubscriber : EndpointConfigurationBuilder
        {
            public PublisherAndSubscriber()
            {
                EndpointSetup<DefaultPublisher>(b =>
                {
                    // Make sure the subscription message isn't purged on startup
                    b.PurgeOnStartup(false);
                    b.OnEndpointSubscribed<Context>((s, context) =>
                    {
                        if (s.MessageType == typeof(Event).AssemblyQualifiedName)
                        {
                            context.EventSubscribed = true;
                        }
                    });
                }, metadata => metadata.RegisterPublisherFor<Event>(typeof(PublisherAndSubscriber)));
            }

            public class EventHandler : IHandleMessages<Event>
            {
                public EventHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(Event @event, IMessageHandlerContext context)
                {
                    if (@event.ContextId != testContext.Id)
                    {
                        return Task.FromResult(0);
                    }
                    testContext.GotEvent = true;

                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class Event : IEvent
        {
            public Guid ContextId { get; set; }
        }
    }
}