namespace NServiceBus.AcceptanceTests.Outbox;

using System;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using Features;
using NServiceBus.Pipeline;
using NUnit.Framework;

public class When_publishing_with_outbox : NServiceBusAcceptanceTest
{
    [Test, CancelAfter(10_000)]
    public async Task Should_be_delivered_to_all_subscribers(CancellationToken cancellationToken = default)
    {
        Requires.OutboxPersistence();

        Context context = await Scenario.Define<Context>()
            .WithEndpoint<Publisher>(b =>
                b.When(c => c.Subscriber1Subscribed && c.Subscriber2Subscribed, (session, c) =>
                {
                    // Send a trigger message that will invoke the handler method that publishes the event
                    c.AddTrace("Both subscribers are subscribed, going to send TriggerMessage");
                    return session.SendLocal(new TriggerMessage());
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
            .Run(cancellationToken);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Subscriber1GotTheEvent, Is.True);
            Assert.That(context.Subscriber2GotTheEvent, Is.True);
        }
    }

    public class Context : ScenarioContext
    {
        public bool Subscriber1GotTheEvent { get; set; }
        public bool Subscriber2GotTheEvent { get; set; }
        public bool Subscriber1Subscribed { get; set; }
        public bool Subscriber2Subscribed { get; set; }

        public void MaybeCompleted() => MarkAsCompleted(Subscriber1GotTheEvent, Subscriber2GotTheEvent);
    }

    public class Publisher : EndpointConfigurationBuilder
    {
        public Publisher() =>
            EndpointSetup<DefaultPublisher>(b =>
            {
                b.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;
                b.EnableOutbox();
                // Test the outbox behavior in situations where messages are deserialized and dispatched from the outbox storage by injecting an exception into the dispatch pipeline
                b.Pipeline.Register(new BlowUpAfterDispatchBehavior(), "ensure outbox dispatch fails");
                b.Recoverability().Immediate(i => i.NumberOfRetries(1));
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

        public class TriggerHandler : IHandleMessages<TriggerMessage>
        {
            public Task Handle(TriggerMessage message, IMessageHandlerContext context)
                => context.Publish(new MyEvent());
        }

        class BlowUpAfterDispatchBehavior : IBehavior<IBatchDispatchContext, IBatchDispatchContext>
        {
            public async Task Invoke(IBatchDispatchContext context, Func<IBatchDispatchContext, Task> next)
            {
                if (Interlocked.Increment(ref invocationCounter) == 1)
                {
                    throw new SimulatedException();
                }

                await next(context).ConfigureAwait(false);
            }

            int invocationCounter;
        }
    }

    public class Subscriber1 : EndpointConfigurationBuilder
    {
        public Subscriber1() =>
            EndpointSetup<DefaultServer>(c => c.DisableFeature<AutoSubscribe>(),
                metadata => metadata.RegisterPublisherFor<MyEvent, Publisher>());

        public class MyHandler(Context testContext) : IHandleMessages<MyEvent>
        {
            public Task Handle(MyEvent message, IMessageHandlerContext context)
            {
                testContext.Subscriber1GotTheEvent = true;
                testContext.MaybeCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class Subscriber2 : EndpointConfigurationBuilder
    {
        public Subscriber2() =>
            EndpointSetup<DefaultServer>(c => c.DisableFeature<AutoSubscribe>(),
                metadata => metadata.RegisterPublisherFor<MyEvent, Publisher>());

        public class MyHandler(Context testContext) : IHandleMessages<MyEvent>
        {
            public Task Handle(MyEvent messageThatIsEnlisted, IMessageHandlerContext context)
            {
                testContext.Subscriber2GotTheEvent = true;
                testContext.MaybeCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class MyEvent : IEvent;

    public class TriggerMessage : ICommand;
}