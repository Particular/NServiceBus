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

    public class When_publishing_pre_created_interface : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_publish_event_to_interface_type_subscribers()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Publisher>(b =>
                    b.When(c => c.Subscribed, (session, ctx) => session.SendLocal(new StartMessage())))
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
                EndpointSetup<DefaultPublisher>((c, r) =>
                {
                    c.Pipeline.Register("EventTypeSpy", new EventTypeSpy((Context)r.ScenarioContext), "EventTypeSpy");
                    c.OnEndpointSubscribed<Context>((s, context) =>
                    {
                        if (s.SubscriberEndpoint.Contains(Conventions.EndpointNamingConvention(typeof(Subscriber))))
                        {
                            context.Subscribed = true;
                        }
                    });
                });
            }

            public class StartMessageHandler : IHandleMessages<StartMessage>
            {
                public StartMessageHandler(IMessageCreator messageCreator)
                {
                    this.messageCreator = messageCreator;
                }

                public Task Handle(StartMessage message, IMessageHandlerContext context)
                {
                    return context.Publish(messageCreator.CreateInstance<IMyEvent>());
                }

                IMessageCreator messageCreator;
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
        public class StartMessage : IMessage
        {
        }
        public interface IMyEvent : IEvent
        {
        }
    }
}
