namespace NServiceBus.AcceptanceTests.Core.AutomaticSubscriptions;

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
            .Run();

        Assert.That(context.EventsSubscribedTo, Has.Count.EqualTo(1));
        Assert.That(context.EventsSubscribedTo, Does.Contain(typeof(MyEvent).AssemblyQualifiedName), "Events should be auto subscribed");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.EventsSubscribedTo.Contains(typeof(MyEventWithNoRouting).AssemblyQualifiedName), Is.False, "Events without routing should not be auto subscribed");
            Assert.That(context.EventsSubscribedTo.Contains(typeof(MyEventWithNoHandler).AssemblyQualifiedName), Is.False, "Events without handlers should not be auto subscribed");
            Assert.That(context.EventsSubscribedTo.Contains(typeof(MyCommand).AssemblyQualifiedName), Is.False, "Commands should not be auto subscribed");
            Assert.That(context.EventsSubscribedTo.Contains(typeof(MyMessage).AssemblyQualifiedName), Is.False, "Plain messages should not be auto subscribed by default");
        }
    }

    class Context : ScenarioContext
    {
        public List<string> EventsSubscribedTo { get; } = [];

        public void MaybeComplete() => MarkAsCompleted(EventsSubscribedTo.Count >= 1);
    }

    class Subscriber : EndpointConfigurationBuilder
    {
        public Subscriber() =>
            EndpointSetup<DefaultServer>((c, r) =>
                {
                    c.ConfigureRouting().RouteToEndpoint(typeof(MyMessage), typeof(Subscriber)); //just route to our self for this test
                    c.ConfigureRouting().RouteToEndpoint(typeof(MyCommand), typeof(Subscriber)); //just route to our self for this test

                    // we can't check the context.Events on the SubscribeContext as events with no route are also contained. Instead we need to check which subscription messages were sent to the configured publisher.
                    c.OnEndpointSubscribed<Context>((subscription, ctx) =>
                    {
                        ctx.EventsSubscribedTo.Add(subscription.MessageType);
                        ctx.MaybeComplete();
                    });
                },
                metadata =>
                {
                    metadata.RegisterPublisherFor<MyEvent, Subscriber>();
                    metadata.RegisterPublisherFor<MyEventWithNoHandler, Subscriber>();
                });

        class MyMessageHandler : IHandleMessages<MyMessage>
        {
            public Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
        }

        public class EventMessageHandler : IHandleMessages<MyEvent>
        {
            public Task Handle(MyEvent message, IMessageHandlerContext context) => Task.CompletedTask;
        }

        public class MyEventWithNoRoutingHandler : IHandleMessages<MyEventWithNoRouting>
        {
            public Task Handle(MyEventWithNoRouting message, IMessageHandlerContext context) => Task.CompletedTask;
        }

        public class CommandMessageHandler : IHandleMessages<MyCommand>
        {
            public Task Handle(MyCommand message, IMessageHandlerContext context) => Task.CompletedTask;
        }
    }

    public class MyMessage : IMessage;

    public class MyCommand : ICommand;

    public class MyEvent : IEvent;

    public class MyEventWithNoRouting : IEvent;

    public class MyEventWithNoHandler : IEvent;
}