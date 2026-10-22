namespace NServiceBus.AcceptanceTests.Core.Pipeline;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Features;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Pipeline;
using NUnit.Framework;
using Conventions = AcceptanceTesting.Customization.Conventions;

public class When_reading_headers_after_multicast_publish_with_header_pooling : NServiceBusAcceptanceTest
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task Should_read_headers_after_next_in_outgoing_behavior(bool enableHeaderPooling)
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Publisher>(b => b
                .CustomConfig(c =>
                {
                    if (enableHeaderPooling)
                    {
                        c.EnableHeaderPooling();
                    }
                })
                .When(c => c.FirstSubscriberSubscribed && c.SecondSubscriberSubscribed, session => session.Publish(new MyEvent())))
            .WithEndpoint<FirstSubscriber>(b => b.When(async (session, ctx) =>
            {
                await session.Subscribe<MyEvent>();
                if (ctx.HasNativePubSubSupport)
                {
                    ctx.FirstSubscriberSubscribed = true;
                }
            }))
            .WithEndpoint<SecondSubscriber>(b => b.When(async (session, ctx) =>
            {
                await session.Subscribe<MyEvent>();
                if (ctx.HasNativePubSubSupport)
                {
                    ctx.SecondSubscriberSubscribed = true;
                }
            }))
            .Done(c => c.FirstSubscriberReceivedEvent && c.SecondSubscriberReceivedEvent && c.BehaviorCompleted)
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.ExceptionAfterNext, Is.Null, $"Reading headers after next() threw: {context.ExceptionAfterNext}");
            Assert.That(context.MessageIdAfterNext, Is.EqualTo(context.MessageIdBeforeNext));
            Assert.That(context.FirstSubscriberMessageId, Is.EqualTo(context.MessageIdBeforeNext));
            Assert.That(context.SecondSubscriberMessageId, Is.EqualTo(context.MessageIdBeforeNext));
        }
    }

    public class Context : ScenarioContext
    {
        public bool FirstSubscriberSubscribed { get; set; }
        public bool SecondSubscriberSubscribed { get; set; }
        public bool FirstSubscriberReceivedEvent { get; set; }
        public bool SecondSubscriberReceivedEvent { get; set; }
        public string FirstSubscriberMessageId { get; set; }
        public string SecondSubscriberMessageId { get; set; }
        public string MessageIdBeforeNext { get; set; }
        public string MessageIdAfterNext { get; set; }
        public Exception ExceptionAfterNext { get; set; }
        public bool BehaviorCompleted { get; set; }
    }

    public class Publisher : EndpointConfigurationBuilder
    {
        public Publisher() => EndpointSetup<DefaultPublisher>(c =>
            {
                c.Pipeline.Register(b => new LogAfterPublishBehavior(b.GetService<Context>()), "Reads the outgoing headers after the rest of the pipeline has run");
                c.OnEndpointSubscribed<Context>((s, ctx) =>
                {
                    if (s.SubscriberEndpoint.Contains(Conventions.EndpointNamingConvention(typeof(FirstSubscriber))))
                    {
                        ctx.FirstSubscriberSubscribed = true;
                    }

                    if (s.SubscriberEndpoint.Contains(Conventions.EndpointNamingConvention(typeof(SecondSubscriber))))
                    {
                        ctx.SecondSubscriberSubscribed = true;
                    }
                });
            },
            metadata => metadata.RegisterSelfAsPublisherFor<MyEvent>(this));

        public class LogAfterPublishBehavior(Context testContext) : Behavior<IOutgoingLogicalMessageContext>
        {
            public override async Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
            {
                testContext.MessageIdBeforeNext = context.Headers[Headers.MessageId];

                await next();

                try
                {
                    testContext.MessageIdAfterNext = context.Headers[Headers.MessageId];
                }
                catch (KeyNotFoundException ex)
                {
                    testContext.ExceptionAfterNext = ex;
                }

                testContext.BehaviorCompleted = true;
            }
        }
    }

    public class FirstSubscriber : EndpointConfigurationBuilder
    {
        public FirstSubscriber() => EndpointSetup<DefaultServer>(c => c.DisableFeature<AutoSubscribe>(),
            metadata => metadata.RegisterPublisherFor<MyEvent, Publisher>());

        [Handler]
        public class MyHandler(Context testContext) : IHandleMessages<MyEvent>
        {
            public Task Handle(MyEvent message, IMessageHandlerContext context)
            {
                testContext.FirstSubscriberMessageId = context.MessageId;
                testContext.FirstSubscriberReceivedEvent = true;
                return Task.CompletedTask;
            }
        }
    }

    public class SecondSubscriber : EndpointConfigurationBuilder
    {
        public SecondSubscriber() => EndpointSetup<DefaultServer>(c => c.DisableFeature<AutoSubscribe>(),
            metadata => metadata.RegisterPublisherFor<MyEvent, Publisher>());

        [Handler]
        public class MyHandler(Context testContext) : IHandleMessages<MyEvent>
        {
            public Task Handle(MyEvent message, IMessageHandlerContext context)
            {
                testContext.SecondSubscriberMessageId = context.MessageId;
                testContext.SecondSubscriberReceivedEvent = true;
                return Task.CompletedTask;
            }
        }
    }

    public class MyEvent : IEvent;
}
