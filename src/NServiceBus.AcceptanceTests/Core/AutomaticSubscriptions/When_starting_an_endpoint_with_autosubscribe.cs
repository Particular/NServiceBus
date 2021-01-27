namespace NServiceBus.AcceptanceTests.Core.AutomaticSubscriptions
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
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
                .Done(c => c.EndpointsStarted && c.EventsSubscribedTo.Count >= 1)
                .Run();

            Assert.AreEqual(1, context.EventsSubscribedTo.Count);
            Assert.True(context.EventsSubscribedTo.Contains(typeof(MyEvent).AssemblyQualifiedName), "Events should be auto subscribed");
            Assert.False(context.EventsSubscribedTo.Contains(typeof(MyEventWithNoRouting).AssemblyQualifiedName), "Events without routing should not be auto subscribed");
            Assert.False(context.EventsSubscribedTo.Contains(typeof(MyEventWithNoHandler).AssemblyQualifiedName), "Events without handlers should not be auto subscribed");
            Assert.False(context.EventsSubscribedTo.Contains(typeof(MyCommand).AssemblyQualifiedName), "Commands should not be auto subscribed");
            Assert.False(context.EventsSubscribedTo.Contains(typeof(MyMessage).AssemblyQualifiedName), "Plain messages should not be auto subscribed by default");
        }

        class Context : ScenarioContext
        {
            public Context()
            {
                EventsSubscribedTo = new List<string>();
            }

            public List<string> EventsSubscribedTo { get; }
        }

        class Subscriber : EndpointConfigurationBuilder
        {
            public Subscriber()
            {
                EndpointSetup<DefaultServer>((c, r) =>
                    {
                        c.ConfigureRouting().RouteToEndpoint(typeof(MyMessage), typeof(Subscriber)); //just route to our self for this test
                        c.ConfigureRouting().RouteToEndpoint(typeof(MyCommand), typeof(Subscriber)); //just route to our self for this test

                        // we can't check the context.Events on the SubscribeContext as events with no route are also contained. Instead we need to check which subscription messages were sent to the configured publisher.
                        c.OnEndpointSubscribed<Context>((subscription, ctx) =>
                            ctx.EventsSubscribedTo.Add(subscription.MessageType));
                    },
                    metadata =>
                    {
                        metadata.RegisterPublisherFor<MyEvent>(typeof(Subscriber));
                        metadata.RegisterPublisherFor<MyEventWithNoHandler>(typeof(Subscriber));
                    });
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