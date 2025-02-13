﻿namespace NServiceBus.AcceptanceTests.Core.Pipeline;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Extensibility;
using Features;
using NServiceBus.Pipeline;
using NUnit.Framework;

public class When_extending_the_publish_api : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_make_the_context_available_to_behaviors()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Publisher>(b =>
                b.When(c => c.Subscriber1Subscribed, session =>
                {
                    var options = new PublishOptions();

                    options.GetExtensions().Set(new Publisher.PublishExtensionBehavior.Context
                    {
                        SomeProperty = "ItWorks"
                    });

                    return session.Publish(new MyEvent(), options);
                })
            )
            .WithEndpoint<Subscriber1>(b => b.When(async (session, ctx) =>
            {
                await session.Subscribe<MyEvent>();

                if (ctx.HasNativePubSubSupport)
                {
                    ctx.Subscriber1Subscribed = true;
                }
            }))
            .Done(c => c.Subscriber1GotTheEvent)
            .Run();

        Assert.That(context.Subscriber1GotTheEvent, Is.True);
    }

    public class Context : ScenarioContext
    {
        public bool Subscriber1GotTheEvent { get; set; }
        public bool Subscriber1Subscribed { get; set; }
    }

    public class Publisher : EndpointConfigurationBuilder
    {
        public Publisher()
        {
            EndpointSetup<DefaultPublisher>(b =>
            {
                b.OnEndpointSubscribed<Context>((s, context) => { context.Subscriber1Subscribed = true; });

                b.Pipeline.Register("PublishExtensionBehavior", new PublishExtensionBehavior(), "Testing publish extensions");
            });
        }

        public class PublishExtensionBehavior : IBehavior<IOutgoingLogicalMessageContext, IOutgoingLogicalMessageContext>
        {
            public Task Invoke(IOutgoingLogicalMessageContext context, Func<IOutgoingLogicalMessageContext, Task> next)
            {
                if (context.Extensions.TryGet(out Context data))
                {
                    Assert.That(data.SomeProperty, Is.EqualTo("ItWorks"));
                }
                else
                {
                    Assert.Fail("Expected to find the data");
                }

                return next(context);
            }

            public class Context
            {
                public string SomeProperty { get; set; }
            }
        }
    }

    public class Subscriber1 : EndpointConfigurationBuilder
    {
        public Subscriber1()
        {
            EndpointSetup<DefaultServer>(builder => builder.DisableFeature<AutoSubscribe>(), metadata => metadata.RegisterPublisherFor<MyEvent, Publisher>());
        }

        public class MyHandler : IHandleMessages<MyEvent>
        {
            public MyHandler(Context context)
            {
                testContext = context;
            }

            public Task Handle(MyEvent messageThatIsEnlisted, IMessageHandlerContext context)
            {
                testContext.Subscriber1GotTheEvent = true;
                return Task.CompletedTask;
            }

            Context testContext;
        }
    }

    public class MyEvent : IEvent
    {
    }
}