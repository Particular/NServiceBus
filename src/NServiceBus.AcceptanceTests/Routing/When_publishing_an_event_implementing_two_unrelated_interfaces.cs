﻿namespace NServiceBus.AcceptanceTests.Routing;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Features;
using NUnit.Framework;
using Conventions = AcceptanceTesting.Customization.Conventions;

public class When_publishing_an_event_implementing_two_unrelated_interfaces : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Event_should_be_published_using_instance_type()
    {
        var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
            .WithEndpoint<Publisher>(b =>
                b.When(c => c.EventASubscribed && c.EventBSubscribed, (session, ctx) =>
                {
                    var message = new CompositeEvent
                    {
                        ContextId = ctx.Id
                    };
                    return session.Publish(message);
                }))
            .WithEndpoint<Subscriber>(b => b.When(async (session, ctx) =>
            {
                await session.Subscribe<IEventA>();
                await session.Subscribe<IEventB>();

                if (ctx.HasNativePubSubSupport)
                {
                    ctx.EventASubscribed = true;
                    ctx.EventBSubscribed = true;
                }
            }))
            .Done(c => c.GotEventA && c.GotEventB)
            .Run(TimeSpan.FromSeconds(20));

        Assert.Multiple(() =>
        {
            Assert.That(context.GotEventA, Is.True);
            Assert.That(context.GotEventB, Is.True);
        });
    }

    public class Context : ScenarioContext
    {
        public Guid Id { get; set; }
        public bool EventASubscribed { get; set; }
        public bool EventBSubscribed { get; set; }
        public bool GotEventA { get; set; }
        public bool GotEventB { get; set; }
    }

    public class Publisher : EndpointConfigurationBuilder
    {
        public Publisher()
        {
            EndpointSetup<DefaultPublisher>(b =>
            {
                b.OnEndpointSubscribed<Context>((s, context) =>
                {
                    if (s.SubscriberEndpoint.Contains(Conventions.EndpointNamingConvention(typeof(Subscriber))))
                    {
                        if (s.MessageType == typeof(IEventA).AssemblyQualifiedName)
                        {
                            context.EventASubscribed = true;
                        }
                        if (s.MessageType == typeof(IEventB).AssemblyQualifiedName)
                        {
                            context.EventBSubscribed = true;
                        }
                    }
                });
            });
        }
    }

    public class Subscriber : EndpointConfigurationBuilder
    {
        public Subscriber()
        {
            EndpointSetup<DefaultServer>(c =>
                {
                    c.DisableFeature<AutoSubscribe>();
                },
                metadata =>
                {
                    metadata.RegisterPublisherFor<IEventA>(typeof(Publisher));
                    metadata.RegisterPublisherFor<IEventB>(typeof(Publisher));
                });
        }

        public class EventAHandler : IHandleMessages<IEventA>
        {
            public EventAHandler(Context context)
            {
                testContext = context;
            }

            public Task Handle(IEventA @event, IMessageHandlerContext context)
            {
                if (@event.ContextId != testContext.Id)
                {
                    return Task.CompletedTask;
                }
                testContext.GotEventA = true;

                return Task.CompletedTask;
            }

            Context testContext;
        }

        public class EventBHandler : IHandleMessages<IEventB>
        {
            public EventBHandler(Context context)
            {
                testContext = context;
            }

            public Task Handle(IEventB @event, IMessageHandlerContext context)
            {
                if (@event.ContextId != testContext.Id)
                {
                    return Task.CompletedTask;
                }

                testContext.GotEventB = true;

                return Task.CompletedTask;
            }

            Context testContext;
        }
    }

    public class CompositeEvent : IEventA, IEventB
    {
        public Guid ContextId { get; set; }
        public string StringProperty { get; set; }
        public int IntProperty { get; set; }
    }

    public interface IEventA : IEvent
    {
        Guid ContextId { get; set; }
        string StringProperty { get; set; }
    }

    public interface IEventB : IEvent
    {
        Guid ContextId { get; set; }
        int IntProperty { get; set; }
    }
}