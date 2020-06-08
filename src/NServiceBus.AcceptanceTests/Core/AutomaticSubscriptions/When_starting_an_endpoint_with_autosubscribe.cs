namespace NServiceBus.AcceptanceTests.Core.AutomaticSubscriptions
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    [TestFixture]
    public class When_starting_an_endpoint_with_autosubscribe : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_autosubscribe_to_relevant_messagetypes()
        {
            Requires.MessageDrivenPubSub();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Subscriber>()
                .Done(c => c.EventsSubscribedTo.Count >= 1)
                .Run();

            Assert.True(context.EventsSubscribedTo.Contains(typeof(MyEvent)), "Events should be auto subscribed");
            Assert.False(context.EventsSubscribedTo.Contains(typeof(MyEventWithNoRouting)), "Events without routing should not be auto subscribed");
            Assert.False(context.EventsSubscribedTo.Contains(typeof(MyEventWithNoHandler)), "Events without handlers should not be auto subscribed");
            Assert.False(context.EventsSubscribedTo.Contains(typeof(MyCommand)), "Commands should not be auto subscribed");
            Assert.False(context.EventsSubscribedTo.Contains(typeof(MyMessage)), "Plain messages should not be auto subscribed by default");
        }

        class Context : ScenarioContext
        {
            public Context()
            {
                EventsSubscribedTo = new List<Type>();
            }

            public List<Type> EventsSubscribedTo { get; }
        }

        class Subscriber : EndpointConfigurationBuilder
        {
            public Subscriber()
            {
                EndpointSetup<DefaultServer>((c, r) =>
                    {
                        c.Pipeline.Register("SubscriptionSpy", new SubscriptionSpy((Context)r.ScenarioContext), "Spies on subscriptions made");
                        c.ConfigureTransport().Routing().RouteToEndpoint(typeof(MyMessage), typeof(Subscriber)); //just route to our self for this test
                        c.ConfigureTransport().Routing().RouteToEndpoint(typeof(MyCommand), typeof(Subscriber)); //just route to our self for this test
                    },
                    metadata =>
                    {
                        metadata.RegisterPublisherFor<MyEvent>(typeof(Subscriber));
                        metadata.RegisterPublisherFor<MyEventWithNoHandler>(typeof(Subscriber));
                    });
            }

            class SubscriptionSpy : IBehavior<ISubscribeContext, ISubscribeContext>
            {
                public SubscriptionSpy(Context testContext)
                {
                    this.testContext = testContext;
                }

                public async Task Invoke(ISubscribeContext context, Func<ISubscribeContext, Task> next)
                {
                    await next(context).ConfigureAwait(false);

                    testContext.EventsSubscribedTo.Add(context.EventType);
                }

                Context testContext;
            }

            class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }
            }

            public class EventMessageHandler : IHandleMessages<MyEvent>
            {
                public Task Handle(MyEvent message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }
            }

            public class MyEventWithNoRoutingHandler : IHandleMessages<MyEventWithNoRouting>
            {
                public Task Handle(MyEventWithNoRouting message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }
            }

            public class CommandMessageHandler : IHandleMessages<MyCommand>
            {
                public Task Handle(MyCommand message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }
            }
        }

        public class MyMessage : IMessage
        {
        }

        public class MyCommand : ICommand
        {
        }

        public class MyEvent : IEvent
        {
        }

        public class MyEventWithNoRouting : IEvent
        {
        }

        public class MyEventWithNoHandler : IEvent
        {
        }
    }
}