namespace NServiceBus.AcceptanceTests.Routing;

using System;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Features;
using NUnit.Framework;
using Conventions = AcceptanceTesting.Customization.Conventions;

public class When_publishing_using_base_type : NServiceBusAcceptanceTest
{
    [Test, CancelAfter(20_000)]
    public async Task Event_should_be_published_using_instance_type(CancellationToken cancellationToken = default)
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Publisher>(b =>
                b.When(c => c.Subscriber1Subscribed, session =>
                {
                    IMyEvent message = new EventMessage();

                    return session.Publish(message);
                }))
            .WithEndpoint<Subscriber1>(b => b.When(async (session, ctx) =>
            {
                await session.Subscribe<EventMessage>();

                if (ctx.HasNativePubSubSupport)
                {
                    ctx.Subscriber1Subscribed = true;
                }
            }))
            .Run(cancellationToken);

        Assert.That(context.Subscriber1GotTheEvent, Is.True);
    }

    public class Context : ScenarioContext
    {
        public bool Subscriber1GotTheEvent { get; set; }
        public bool Subscriber1Subscribed { get; set; }
    }

    public class Publisher : EndpointConfigurationBuilder
    {
        public Publisher() =>
            EndpointSetup<DefaultPublisher>(b => b.OnEndpointSubscribed<Context>((s, context) =>
            {
                if (s.SubscriberEndpoint.Contains(Conventions.EndpointNamingConvention(typeof(Subscriber1))))
                {
                    context.Subscriber1Subscribed = true;
                }
            }), metadata => metadata.RegisterSelfAsPublisherFor<EventMessage>(this));
    }

    public class Subscriber1 : EndpointConfigurationBuilder
    {
        public Subscriber1() => EndpointSetup<DefaultServer>(c => c.DisableFeature<AutoSubscribe>(), p => p.RegisterPublisherFor<EventMessage, Publisher>());

        public class MyHandler(Context testContext) : IHandleMessages<EventMessage>
        {
            public Task Handle(EventMessage messageThatIsEnlisted, IMessageHandlerContext context)
            {
                testContext.Subscriber1GotTheEvent = true;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class EventMessage : IMyEvent
    {
        public Guid EventId { get; set; }
        public DateTime? Time { get; set; }
        public TimeSpan Duration { get; set; }
    }

    public interface IMyEvent : IEvent
    {
        Guid EventId { get; set; }
        DateTime? Time { get; set; }
        TimeSpan Duration { get; set; }
    }
}