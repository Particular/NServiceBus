﻿namespace NServiceBus.AcceptanceTests.Routing
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    public class When_publishing_an_interface : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_receive_event_for_non_xml()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Publisher>(b =>
                    b.When(c => c.Subscribed, (session, ctx) => session.Publish<MyEvent>()))
                .WithEndpoint<Subscriber>(b => b.When(async (session, ctx) =>
                {
                    await session.Subscribe<MyEvent>();
                    if (ctx.HasNativePubSubSupport)
                    {
                        ctx.Subscribed = true;
                    }
                }))
                .Done(c => c.GotTheEvent)
                .Run();

            Assert.True(context.GotTheEvent);
            Assert.AreEqual(typeof(MyEvent), context.EventTypePassedToRouting);
        }

        public class Context : ScenarioContext
        {
            public bool GotTheEvent { get; set; }
            public bool Subscribed { get; set; }
            public Type EventTypePassedToRouting { get; set; }
        }

        public class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultPublisher>(c =>
                {
                    c.UseSerialization<JsonSerializer>();
                    c.Pipeline.Register("EventTypeSpy", new EventTypeSpy((Context)ScenarioContext), "EventTypeSpy");
                    c.OnEndpointSubscribed<Context>((s, context) =>
                    {
                        if (s.SubscriberReturnAddress.Contains("Subscriber"))
                        {
                            context.Subscribed = true;
                        }
                    });
                });
            }

            class EventTypeSpy : IBehavior<IOutgoingLogicalMessageContext, IOutgoingLogicalMessageContext>
            {
                public EventTypeSpy(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Invoke(IOutgoingLogicalMessageContext context, Func<IOutgoingLogicalMessageContext, Task> next)
                {
                    testContext.EventTypePassedToRouting = context.Message.MessageType;
                    return next(context);
                }

                Context testContext;
            }
        }

        public class Subscriber : EndpointConfigurationBuilder
        {
            public Subscriber()
            {
                EndpointSetup<DefaultServer>(c =>
                    {
                        c.UseSerialization<JsonSerializer>();
                        c.DisableFeature<AutoSubscribe>();
                    },
                    metadata => metadata.RegisterPublisherFor<MyEvent>(typeof(Publisher)));
            }

            public class MyEventHandler : IHandleMessages<MyEvent>
            {
                public Context Context { get; set; }

                public Task Handle(MyEvent @event, IMessageHandlerContext context)
                {
                    Context.GotTheEvent = true;
                    return Task.FromResult(0);
                }
            }
        }

        public interface MyEvent : IEvent
        {
        }
    }
}