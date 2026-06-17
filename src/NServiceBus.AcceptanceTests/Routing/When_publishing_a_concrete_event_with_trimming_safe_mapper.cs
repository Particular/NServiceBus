namespace NServiceBus.AcceptanceTests.Routing;

using System;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using Configuration.AdvancedExtensibility;
using EndpointTemplates;
using Features;
using MessageInterfaces.MessageMapper.Reflection;
using NServiceBus.MessageInterfaces;
using NUnit.Framework;
using Conventions = AcceptanceTesting.Customization.Conventions;

public class When_publishing_a_concrete_event_with_trimming_safe_mapper : NServiceBusAcceptanceTest
{
    [Test, CancelAfter(20_000)]
    public async Task Should_dispatch_to_interface_handlers_without_generating_a_proxy(CancellationToken cancellationToken = default)
    {
        var context = await Scenario.Define<Context>(c => c.Id = Guid.NewGuid())
            .WithEndpoint<Publisher>(b =>
                b.When(c => c is { EventASubscribed: true, EventBSubscribed: true }, (session, ctx) =>
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
            .Run(cancellationToken);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.GotEventA, Is.True);
            Assert.That(context.GotEventB, Is.True);
            Assert.That(context.HandlerReceivedConcreteInstance, Is.True);
        }
    }

    public class Context : ScenarioContext
    {
        public Guid Id { get; set; }
        public bool EventASubscribed { get; set; }
        public bool EventBSubscribed { get; set; }
        public bool GotEventA { get; set; }
        public bool GotEventB { get; set; }
        public bool HandlerReceivedConcreteInstance { get; set; }

        public void MaybeCompleted() => MarkAsCompleted(GotEventA, GotEventB);
    }

    public class Publisher : EndpointConfigurationBuilder
    {
        public Publisher() =>
            EndpointSetup<DefaultPublisher>(c =>
                {
                    // Force the trimming-safe mapper even though the test runs on a JIT runtime,
                    // to prove the path that NativeAOT/trimmed endpoints take.
                    c.GetSettings().Set<IMessageMapper>(new TrimmingSafeMessageMapper());
                    c.OnEndpointSubscribed<Context>((s, context) =>
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
                },
                metadata => metadata.RegisterSelfAsPublisherFor<CompositeEvent>(this));
    }

    public class Subscriber : EndpointConfigurationBuilder
    {
        public Subscriber() =>
            EndpointSetup<DefaultServer>(c =>
                {
                    c.DisableFeature<AutoSubscribe>();
                    c.GetSettings().Set<IMessageMapper>(new TrimmingSafeMessageMapper());
                },
                metadata =>
                {
                    metadata.RegisterPublisherFor<IEventA, Publisher>();
                    metadata.RegisterPublisherFor<IEventB, Publisher>();
                });

        [Handler]
        public class EventAHandler(Context testContext) : IHandleMessages<IEventA>
        {
            public Task Handle(IEventA @event, IMessageHandlerContext context)
            {
                if (@event.ContextId != testContext.Id)
                {
                    return Task.CompletedTask;
                }
                testContext.GotEventA = true;
                testContext.HandlerReceivedConcreteInstance = @event is CompositeEvent;
                testContext.MaybeCompleted();

                return Task.CompletedTask;
            }
        }

        [Handler]
        public class EventBHandler(Context testContext) : IHandleMessages<IEventB>
        {
            public Task Handle(IEventB @event, IMessageHandlerContext context)
            {
                if (@event.ContextId != testContext.Id)
                {
                    return Task.CompletedTask;
                }

                testContext.GotEventB = true;
                testContext.MaybeCompleted();

                return Task.CompletedTask;
            }
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
    }

    public interface IEventB : IEvent
    {
        Guid ContextId { get; set; }
    }
}
