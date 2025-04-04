﻿namespace NServiceBus.AcceptanceTests.Routing;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Features;
using NUnit.Framework;
using Conventions = AcceptanceTesting.Customization.Conventions;

public class When_publishing : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Issue_1851()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Publisher3>(b =>
                b.When(c => c.Subscriber3Subscribed, session => session.Publish<IFoo>())
            )
            .WithEndpoint<Subscriber3>(b => b.When(async (session, ctx) =>
            {
                await session.Subscribe<IFoo>();

                if (ctx.HasNativePubSubSupport)
                {
                    ctx.Subscriber3Subscribed = true;
                }
            }))
            .Done(c => c.Subscriber3GotTheEvent)
            .Run();

        Assert.That(context.Subscriber3GotTheEvent, Is.True);
    }

    [Test]
    public async Task Should_be_delivered_to_all_subscribers()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Publisher>(b =>
                b.When(c => c.Subscriber1Subscribed && c.Subscriber2Subscribed, (session, c) =>
                {
                    c.AddTrace("Both subscribers is subscribed, going to publish MyEvent");

                    var options = new PublishOptions();

                    options.SetHeader("MyHeader", "SomeValue");
                    return session.Publish(new MyEvent(), options);
                })
            )
            .WithEndpoint<Subscriber1>(b => b.When(async (session, ctx) =>
            {
                await session.Subscribe<MyEvent>();
                if (ctx.HasNativePubSubSupport)
                {
                    ctx.Subscriber1Subscribed = true;
                    ctx.AddTrace("Subscriber1 is now subscribed (at least we have asked the broker to be subscribed)");
                }
                else
                {
                    ctx.AddTrace("Subscriber1 has now asked to be subscribed to MyEvent");
                }
            }))
            .WithEndpoint<Subscriber2>(b => b.When(async (session, ctx) =>
            {
                await session.Subscribe<MyEvent>();

                if (ctx.HasNativePubSubSupport)
                {
                    ctx.Subscriber2Subscribed = true;
                    ctx.AddTrace("Subscriber2 is now subscribed (at least we have asked the broker to be subscribed)");
                }
                else
                {
                    ctx.AddTrace("Subscriber2 has now asked to be subscribed to MyEvent");
                }
            }))
            .Done(c => c.Subscriber1GotTheEvent && c.Subscriber2GotTheEvent)
            .Run(TimeSpan.FromSeconds(10));

        Assert.Multiple(() =>
        {
            Assert.That(context.Subscriber1GotTheEvent, Is.True);
            Assert.That(context.Subscriber2GotTheEvent, Is.True);
            Assert.That(context.HeaderValue, Is.EqualTo("SomeValue"));
        });
    }

    public class Context : ScenarioContext
    {
        public bool Subscriber1GotTheEvent { get; set; }
        public bool Subscriber2GotTheEvent { get; set; }
        public bool Subscriber3GotTheEvent { get; set; }
        public bool Subscriber1Subscribed { get; set; }
        public bool Subscriber2Subscribed { get; set; }
        public bool Subscriber3Subscribed { get; set; }
        public string HeaderValue { get; set; }
    }

    public class Publisher : EndpointConfigurationBuilder
    {
        public Publisher()
        {
            EndpointSetup<DefaultPublisher>(b =>
            {
                b.OnEndpointSubscribed<Context>((s, context) =>
                {
                    var subscriber1 = Conventions.EndpointNamingConvention(typeof(Subscriber1));
                    if (s.SubscriberEndpoint.Contains(subscriber1))
                    {
                        context.Subscriber1Subscribed = true;
                        context.AddTrace($"{subscriber1} is now subscribed");
                    }

                    var subscriber2 = Conventions.EndpointNamingConvention(typeof(Subscriber2));
                    if (s.SubscriberEndpoint.Contains(subscriber2))
                    {
                        context.AddTrace($"{subscriber2} is now subscribed");
                        context.Subscriber2Subscribed = true;
                    }
                });
                b.DisableFeature<AutoSubscribe>();
            }, metadata => metadata.RegisterSelfAsPublisherFor<MyEvent>(this));
        }
    }

    public class Publisher3 : EndpointConfigurationBuilder
    {
        public Publisher3()
        {
            EndpointSetup<DefaultPublisher>(b => b.OnEndpointSubscribed<Context>((s, context) =>
            {
                var subscriber3 = Conventions.EndpointNamingConvention(typeof(Subscriber3));
                if (s.SubscriberEndpoint.Contains(subscriber3))
                {
                    context.AddTrace($"{subscriber3} is now subscribed");
                    context.Subscriber3Subscribed = true;
                }
            }), metadata => metadata.RegisterSelfAsPublisherFor<IFoo>(this));
        }
    }

    public class Subscriber3 : EndpointConfigurationBuilder
    {
        public Subscriber3()
        {
            EndpointSetup<DefaultServer>(c => c.DisableFeature<AutoSubscribe>(),
                metadata => metadata.RegisterPublisherFor<IFoo, Publisher3>());
        }

        public class MyHandler : IHandleMessages<IFoo>
        {
            public MyHandler(Context context)
            {
                testContext = context;
            }

            public Task Handle(IFoo messageThatIsEnlisted, IMessageHandlerContext context)
            {
                testContext.Subscriber3GotTheEvent = true;
                return Task.CompletedTask;
            }

            Context testContext;
        }
    }

    public class Subscriber1 : EndpointConfigurationBuilder
    {
        public Subscriber1()
        {
            EndpointSetup<DefaultServer>(c => c.DisableFeature<AutoSubscribe>(),
                 metadata => metadata.RegisterPublisherFor<MyEvent, Publisher>());
        }

        public class MyHandler : IHandleMessages<MyEvent>
        {
            public MyHandler(Context context)
            {
                testContext = context;
            }

            public Task Handle(MyEvent message, IMessageHandlerContext context)
            {
                testContext.HeaderValue = context.MessageHeaders["MyHeader"];
                testContext.Subscriber1GotTheEvent = true;
                return Task.CompletedTask;
            }

            Context testContext;
        }
    }

    public class Subscriber2 : EndpointConfigurationBuilder
    {
        public Subscriber2()
        {
            EndpointSetup<DefaultServer>(c => c.DisableFeature<AutoSubscribe>(),
                metadata => metadata.RegisterPublisherFor<MyEvent, Publisher>());
        }

        public class MyHandler : IHandleMessages<MyEvent>
        {
            public MyHandler(Context context)
            {
                testContext = context;
            }

            public Task Handle(MyEvent messageThatIsEnlisted, IMessageHandlerContext context)
            {
                testContext.Subscriber2GotTheEvent = true;
                return Task.CompletedTask;
            }

            Context testContext;
        }
    }

    public interface IFoo : IEvent
    {
    }


    public class MyEvent : IEvent
    {
    }
}