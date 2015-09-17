namespace NServiceBus.AcceptanceTests.Routing.AutomaticSubscriptions
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NUnit.Framework;

    [TestFixture]
    public class When_starting_an_endpoint_with_autosubscribe : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_autosubscribe_to_relevant_messagetypes()
        {
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

        [Test]
        public async Task Should_autosubscribe_plain_messages_if_asked_to()
        {
            var context = await Scenario.Define<Context>()
               .WithEndpoint<Subscriber>(b => b.CustomConfig(c => c.AutoSubscribe().AutoSubscribePlainMessages()))
               .Done(c => c.EventsSubscribedTo.Count >= 2)
               .Run();

            Assert.True(context.EventsSubscribedTo.Contains(typeof(MyEvent)), "Events should be auto subscribed");
            Assert.False(context.EventsSubscribedTo.Contains(typeof(MyEventWithNoRouting)), "Events without routing should not be auto subscribed");
            Assert.False(context.EventsSubscribedTo.Contains(typeof(MyEventWithNoHandler)), "Events without handlers should not be auto subscribed");
            Assert.False(context.EventsSubscribedTo.Contains(typeof(MyCommand)), "Commands should not be auto subscribed");
            Assert.True(context.EventsSubscribedTo.Contains(typeof(MyMessage)), "Plain messages should be auto subscribed by if asked to");
        }

        class Context : ScenarioContext
        {
            public Context()
            {
                EventsSubscribedTo = new List<Type>();
            }
            public List<Type> EventsSubscribedTo { get; set; }
        }

        class Subscriber : EndpointConfigurationBuilder
        {
            public Subscriber()
            {
                EndpointSetup<DefaultServer>(c => c.Pipeline.Register("SubscriptionSpy", typeof(SubscriptionSpy), "Spies on subscriptions made"))
                    .AddMapping<MyMessage>(typeof(Subscriber)) //just map to our self for this test
                    .AddMapping<MyCommand>(typeof(Subscriber)) //just map to our self for this test
                    .AddMapping<MyEventWithNoHandler>(typeof(Subscriber)) //just map to our self for this test
                    .AddMapping<MyEvent>(typeof(Subscriber)); //just map to our self for this test
            }

            public class SubscriptionSpy : Behavior<SubscribeContext>
            {
                public SubscriptionSpy(Context testContext)
                {
                    this.testContext = testContext;
                }

                public override async Task Invoke(SubscribeContext context, Func<Task> next)
                {
                    await next().ConfigureAwait(false);

                    testContext.EventsSubscribedTo.Add(context.EventType);
                }

                Context testContext;
            }


            class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Task Handle(MyMessage message)
                {
                    return Task.FromResult(0);
                }
            }


            public class EventMessageHandler : IHandleMessages<MyEvent>
            {
                public Task Handle(MyEvent message)
                {
                    return Task.FromResult(0);
                }
            }
            public class MyEventWithNoRoutingHandler : IHandleMessages<MyEventWithNoRouting>
            {
                public Task Handle(MyEventWithNoRouting message)
                {
                    return Task.FromResult(0);
                }
            }

            public class CommandMessageHandler : IHandleMessages<MyCommand>
            {
                public Task Handle(MyCommand message)
                {
                    return Task.FromResult(0);
                }
            }
        }

        class MyMessage : IMessage { }
        class MyCommand : ICommand { }
        class MyEvent : IEvent { }
        class MyEventWithNoRouting : IEvent { }
        class MyEventWithNoHandler : IEvent { }
    }
}