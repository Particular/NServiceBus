namespace NServiceBus.AcceptanceTests.Routing.AutomaticSubscriptions
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NServiceBus.Pipeline;
    using NUnit.Framework;
    using ScenarioDescriptors;

    [TestFixture]
    public class When_starting_an_endpoint_with_autosubscribe : NServiceBusAcceptanceTest
    {
        [Test]
        public Task Should_autosubscribe_to_relevant_messagetypes()
        {
            return Scenario.Define<Context>()
                .WithEndpoint<Subscriber>()
                .Done(c => c.EventsSubscribedTo.Count >= 1)
                .Repeat(b => b.For<AllTransportsWithMessageDrivenPubSub>())
                .Should(ctx => Assert.True(ctx.EventsSubscribedTo.Contains(typeof(MyEvent)), "Events should be auto subscribed"))
                .Should(ctx => Assert.False(ctx.EventsSubscribedTo.Contains(typeof(MyEventWithNoRouting)), "Events without routing should not be auto subscribed"))
                .Should(ctx => Assert.False(ctx.EventsSubscribedTo.Contains(typeof(MyEventWithNoHandler)), "Events without handlers should not be auto subscribed"))
                .Should(ctx => Assert.False(ctx.EventsSubscribedTo.Contains(typeof(MyCommand)), "Commands should not be auto subscribed"))
                .Should(ctx => Assert.False(ctx.EventsSubscribedTo.Contains(typeof(MyMessage)), "Plain messages should not be auto subscribed by default"))
                .Run();
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
                EndpointSetup<DefaultServer>(c => c.Pipeline.Register("SubscriptionSpy", new SubscriptionSpy((Context) ScenarioContext), "Spies on subscriptions made"),
                        metadata =>
                        {
                            metadata.RegisterPublisherFor<MyEvent>(typeof(Subscriber));
                            metadata.RegisterPublisherFor<MyEventWithNoHandler>(typeof(Subscriber));
                        })
                    .AddMapping<MyMessage>(typeof(Subscriber)) //just map to our self for this test
                    .AddMapping<MyCommand>(typeof(Subscriber)); //just map to our self for this test
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