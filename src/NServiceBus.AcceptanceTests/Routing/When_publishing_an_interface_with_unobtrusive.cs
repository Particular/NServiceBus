namespace NServiceBus.AcceptanceTests.Routing;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Features;
using NServiceBus.Pipeline;
using NUnit.Framework;
using Conventions = AcceptanceTesting.Customization.Conventions;

public class When_publishing_an_interface_with_unobtrusive : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_receive_event()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Publisher>(b =>
                b.When(c => c.Subscribed, (session, ctx) => session.Publish<IMyEvent>()))
            .WithEndpoint<Subscriber>(b => b.When(async (session, ctx) =>
            {
                await session.Subscribe<IMyEvent>();
                if (ctx.HasNativePubSubSupport)
                {
                    ctx.Subscribed = true;
                }
            }))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.GotTheEvent, Is.True);
            Assert.That(context.EventTypePassedToRouting, Is.EqualTo(typeof(IMyEvent)));
        }
    }

    public class Context : ScenarioContext
    {
        public bool GotTheEvent { get; set; }
        public bool Subscribed { get; set; }
        public Type EventTypePassedToRouting { get; set; }
    }

    public class Publisher : EndpointConfigurationBuilder
    {
        public Publisher() =>
            EndpointSetup<DefaultPublisher>(c =>
            {
                c.Conventions().DefiningEventsAs(t => t.Namespace != null && t.Name.EndsWith("Event"));
                c.Pipeline.Register("EventTypeSpy", typeof(EventTypeSpy), "EventTypeSpy");
                c.OnEndpointSubscribed<Context>((s, context) =>
                {
                    if (s.SubscriberEndpoint.Contains(Conventions.EndpointNamingConvention(typeof(Subscriber))))
                    {
                        context.Subscribed = true;
                    }
                });
            }, metadata => metadata.RegisterSelfAsPublisherFor<IMyEvent>(this));

        class EventTypeSpy(Context testContext) : IBehavior<IOutgoingLogicalMessageContext, IOutgoingLogicalMessageContext>
        {
            public Task Invoke(IOutgoingLogicalMessageContext context, Func<IOutgoingLogicalMessageContext, Task> next)
            {
                testContext.EventTypePassedToRouting = context.Message.MessageType;
                return next(context);
            }
        }
    }

    public class Subscriber : EndpointConfigurationBuilder
    {
        public Subscriber() =>
            EndpointSetup<DefaultServer>(c =>
                {
                    c.Conventions().DefiningEventsAs(t => t.Namespace != null && t.Name.EndsWith("Event"));
                    c.DisableFeature<AutoSubscribe>();
                },
                metadata => metadata.RegisterPublisherFor<IMyEvent, Publisher>());

        public class MyHandler(Context testContext) : IHandleMessages<IMyEvent>
        {
            public Task Handle(IMyEvent @event, IMessageHandlerContext context)
            {
                testContext.GotTheEvent = true;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public interface IMyEvent;
}