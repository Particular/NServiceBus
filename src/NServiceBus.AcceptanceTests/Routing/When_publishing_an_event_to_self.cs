namespace NServiceBus.AcceptanceTests.Routing
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_publishing_an_event_to_self : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Event_should_be_received()
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
                .Run(TimeSpan.FromSeconds(20)).ConfigureAwait(false);

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
                    b.OnEndpointSubscribed<Context>((s, context) =>
                    {
                        if (s.MessageType == typeof(Event).AssemblyQualifiedName)
                        {
                            context.EventSubscribed = true;
                        }
                    });
                })
                .AddMapping<Event>(typeof(PublisherAndSubscriber));

            }

            public class EventHandler : IHandleMessages<Event>
            {
                public Context Context { get; set; }

                public Task Handle(Event @event, IMessageHandlerContext context)
                {
                    if (@event.ContextId != Context.Id)
                    {
                        return Task.FromResult(0);
                    }
                    Context.GotEvent = true;

                    return Task.FromResult(0);
                }
            }
        }

        public class Event : IEvent
        {
            public Guid ContextId { get; set; }
        }
    }
}