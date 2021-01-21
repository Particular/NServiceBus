namespace NServiceBus.AcceptanceTests.Routing
{
    using System;
    using System.Threading;
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
                .Done(c => c.GotTheEvent)
                .Run();

            Assert.True(context.GotTheEvent);
            Assert.AreEqual(typeof(IMyEvent), context.EventTypePassedToRouting);
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
                    c.Conventions().DefiningEventsAs(t => t.Namespace != null && t.Name.EndsWith("Event"));
                    c.Pipeline.Register("EventTypeSpy", typeof(EventTypeSpy), "EventTypeSpy");
                    c.OnEndpointSubscribed<Context>((s, context) =>
                    {
                        if (s.SubscriberEndpoint.Contains(Conventions.EndpointNamingConvention(typeof(Subscriber))))
                        {
                            context.Subscribed = true;
                        }
                    });
                }).ExcludeType<IMyEvent>(); // remove that type from assembly scanning to simulate what would happen with true unobtrusive mode
            }

            class EventTypeSpy : IBehavior<IOutgoingLogicalMessageContext, IOutgoingLogicalMessageContext>
            {
                public EventTypeSpy(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Invoke(IOutgoingLogicalMessageContext context, Func<IOutgoingLogicalMessageContext, CancellationToken, Task> next, CancellationToken token)
                {
                    testContext.EventTypePassedToRouting = context.Message.MessageType;
                    return next(context, token);
                }

                Context testContext;
            }
        }

        public class Subscriber : EndpointConfigurationBuilder
        {
            public Subscriber()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.Conventions().DefiningEventsAs(t => t.Namespace != null && t.Name.EndsWith("Event"));
                    c.DisableFeature<AutoSubscribe>();
                },
                metadata => metadata.RegisterPublisherFor<IMyEvent>(typeof(Publisher)));
            }

            public class MyEventHandler : IHandleMessages<IMyEvent>
            {
                public MyEventHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(IMyEvent @event, IMessageHandlerContext context)
                {
                    testContext.GotTheEvent = true;
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public interface IMyEvent
        {
        }
    }
}