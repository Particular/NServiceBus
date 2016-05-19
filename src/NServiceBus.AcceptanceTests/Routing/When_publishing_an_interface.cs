﻿namespace NServiceBus.AcceptanceTests.Routing
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using Logging;
    using NServiceBus.Pipeline;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_publishing_an_interface : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_receive_event_for_non_xml()
        {
            await Scenario.Define<Context>(c => c.SetLogLevel("NServiceBus.UnicastRoutingTable", LogLevel.Debug))
                .WithEndpoint<Publisher>(b =>
                    b.When(c => c.Subscribed, (session, ctx) => session.Publish<MyEvent>()))
                .WithEndpoint<Subscriber>(b => b.When(async (session, context) =>
                {
                    await session.Subscribe<MyEvent>();
                    if (context.HasNativePubSubSupport)
                    {
                        context.Subscribed = true;
                    }
                }))
                .Done(c => c.GotTheEvent)
                .Repeat(r => r.For(Serializers.Json))
                .Should(c =>
                {
                    Assert.True(c.GotTheEvent);
                    Assert.AreEqual(typeof(MyEvent), c.EventTypePassedToRouting);
                })
                .Run();
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
                    c.Pipeline.Register("EventTypeSpy", typeof(EventTypeSpy), "EventTypeSpy");
                    c.OnEndpointSubscribed<Context>((s, context) =>
                    {
                        if (s.SubscriberReturnAddress.Contains("Subscriber"))
                        {
                            context.Subscribed = true;
                        }
                    });
                });
            }

            class EventTypeSpy : Behavior<IOutgoingLogicalMessageContext>
            {
                public EventTypeSpy(Context testContext)
                {
                    this.testContext = testContext;
                }

                public override Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
                {
                    testContext.EventTypePassedToRouting = context.Message.MessageType;
                    return next();
                }

                Context testContext;
            }
        }

        public class Subscriber : EndpointConfigurationBuilder
        {
            public Subscriber()
            {
                EndpointSetup<DefaultServer>(c => c.DisableFeature<AutoSubscribe>())
                    .AddMapping<MyEvent>(typeof(Publisher));
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