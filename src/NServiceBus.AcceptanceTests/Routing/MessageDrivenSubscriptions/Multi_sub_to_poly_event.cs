﻿namespace NServiceBus.AcceptanceTests.Routing.MessageDrivenSubscriptions;

using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Features;
using NUnit.Framework;

public class MultiSubscribeToPolymorphicEvent : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Both_events_should_be_delivered()
    {
        Requires.MessageDrivenPubSub();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<Publisher1>(b => b.When(c => c.Publisher1HasASubscriberForIMyEvent, (session, c) =>
            {
                c.AddTrace("Publishing MyEvent1");
                return session.Publish(new MyEvent1());
            }))
            .WithEndpoint<Publisher2>(b => b.When(c => c.Publisher2HasDetectedASubscriberForEvent2, (session, c) =>
            {
                c.AddTrace("Publishing MyEvent2");
                return session.Publish(new MyEvent2());
            }))
            .WithEndpoint<Subscriber1>(b => b.When(async (session, c) =>
            {
                c.AddTrace("Subscriber1 subscribing to both events");
                await session.Subscribe<IMyEvent>();
                await session.Subscribe<MyEvent2>();
            }))
            .Done(c => c.SubscriberGotIMyEvent && c.SubscriberGotMyEvent2)
            .Run();

        Assert.Multiple(() =>
        {
            Assert.That(context.SubscriberGotIMyEvent, Is.True);
            Assert.That(context.SubscriberGotMyEvent2, Is.True);
        });

    }

    public class Context : ScenarioContext
    {
        public bool SubscriberGotIMyEvent { get; set; }
        public bool SubscriberGotMyEvent2 { get; set; }
        public bool Publisher1HasASubscriberForIMyEvent { get; set; }
        public bool Publisher2HasDetectedASubscriberForEvent2 { get; set; }
    }

    public class Publisher1 : EndpointConfigurationBuilder
    {
        public Publisher1()
        {
            EndpointSetup<DefaultPublisher>(b =>
            {
                //Immediate Retries on since subscription storages can throw on concurrency violation and need to retry
                b.Recoverability().Immediate(immediate => immediate.NumberOfRetries(5));
                b.OnEndpointSubscribed<Context>((args, context) =>
                {
                    context.AddTrace("Publisher1 OnEndpointSubscribed " + args.MessageType);
                    if (args.MessageType.Contains(nameof(IMyEvent)))
                    {
                        context.Publisher1HasASubscriberForIMyEvent = true;
                    }
                });
            }, metadata => metadata.RegisterSelfAsPublisherFor<MyEvent1>(this));
        }
    }

    public class Publisher2 : EndpointConfigurationBuilder
    {
        public Publisher2()
        {
            EndpointSetup<DefaultPublisher>(b =>
            {
                // Immediate Retries on since subscription storages can throw on concurrency violation and need to retry
                b.Recoverability().Immediate(immediate => immediate.NumberOfRetries(5));

                b.OnEndpointSubscribed<Context>((args, context) =>
                {
                    context.AddTrace("Publisher2 OnEndpointSubscribed " + args.MessageType);

                    if (args.MessageType.Contains(nameof(MyEvent2)))
                    {
                        context.Publisher2HasDetectedASubscriberForEvent2 = true;
                    }
                });
            }, metadata => metadata.RegisterSelfAsPublisherFor<MyEvent2>(this));
        }
    }

    public class Subscriber1 : EndpointConfigurationBuilder
    {
        public Subscriber1()
        {
            EndpointSetup<DefaultServer>(c => c.DisableFeature<AutoSubscribe>(),
                    metadata =>
                    {
                        metadata.RegisterPublisherFor<IMyEvent, Publisher1>();
                        metadata.RegisterPublisherFor<MyEvent2, Publisher2>();
                    });
        }

        public class MyHandler : IHandleMessages<IMyEvent>
        {
            public MyHandler(Context context)
            {
                testContext = context;
            }

            public Task Handle(IMyEvent messageThatIsEnlisted, IMessageHandlerContext context)
            {
                testContext.AddTrace($"Got event '{messageThatIsEnlisted}'");
                if (messageThatIsEnlisted is MyEvent2)
                {
                    testContext.SubscriberGotMyEvent2 = true;
                }
                else
                {
                    testContext.SubscriberGotIMyEvent = true;
                }

                return Task.CompletedTask;
            }

            Context testContext;
        }
    }

    public class MyEvent1 : IMyEvent
    {
    }

    public class MyEvent2 : IMyEvent
    {
    }

    public interface IMyEvent : IEvent
    {
    }
}