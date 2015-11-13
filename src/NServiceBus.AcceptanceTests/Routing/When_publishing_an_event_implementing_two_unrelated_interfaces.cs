namespace NServiceBus.AcceptanceTests.Routing
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class When_publishing_an_event_implementing_two_unrelated_interfaces : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Event_should_be_published_using_instance_type()
        {
            await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                    .WithEndpoint<Publisher>(b =>
                        b.When(c => c.EventASubscribed && c.EventBSubscribed, (bus, ctx) =>
                        {
                            var message = new CompositeEvent
                            {
                                ContextId = ctx.Id
                            };
                            return bus.Publish(message);
                        }))
                    .WithEndpoint<Subscriber>(b => b.When(async (bus, context) =>
                    {
                        await bus.Subscribe<IEventA>();
                        await bus.Subscribe<IEventB>();

                        if (context.HasNativePubSubSupport)
                        {
                            context.EventASubscribed = true;
                            context.EventBSubscribed = true;
                        }
                    }))
                    .Done(c => c.GotEventA && c.GotEventB)
                    .Repeat(r => r.For(Serializers.Xml))
                    .Should(c =>
                    {
                        Assert.True(c.GotEventA);
                        Assert.True(c.GotEventB);
                    })
                    .Run(TimeSpan.FromSeconds(20));
        }

        public class Context : ScenarioContext
        {
            public Guid Id { get; set; }
            public bool EventASubscribed { get; set; }
            public bool EventBSubscribed { get; set; }
            public bool GotEventA { get; set; }
            public bool GotEventB { get; set; }
        }

        public class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultPublisher>(b => b.OnEndpointSubscribed<Context>((s, context) =>
                {
                    if (s.SubscriberReturnAddress.Contains("Subscriber"))
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
                }));
            }
        }

        public class Subscriber : EndpointConfigurationBuilder
        {
            public Subscriber()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.Conventions().DefiningMessagesAs(t => t != typeof(CompositeEvent) && typeof(IMessage).IsAssignableFrom(t) &&
                                                            typeof(IMessage) != t &&
                                                            typeof(IEvent) != t &&
                                                            typeof(ICommand) != t);

                    c.Conventions().DefiningEventsAs(t => t != typeof(CompositeEvent) && typeof(IEvent).IsAssignableFrom(t) && typeof(IEvent) != t);
                    c.DisableFeature<AutoSubscribe>();
                })
                    .AddMapping<IEventA>(typeof(Publisher))
                    .AddMapping<IEventB>(typeof(Publisher));
            }

            public class EventAHandler : IHandleMessages<IEventA>
            {
                public Context Context { get; set; }

                public Task Handle(IEventA @event, IMessageHandlerContext context)
                {
                    if (@event.ContextId != Context.Id)
                    {
                        return Task.FromResult(0);
                    }
                    Context.GotEventA = true;

                    return Task.FromResult(0);
                }
            }

            public class EventBHandler : IHandleMessages<IEventB>
            {
                public Context Context { get; set; }

                public Task Handle(IEventB @event, IMessageHandlerContext context)
                {
                    if (@event.ContextId != Context.Id)
                    {
                        return Task.FromResult(0);
                    }

                    Context.GotEventB = true;

                    return Task.FromResult(0);
                }
            }
        }

        public class CompositeEvent : IEventA, IEventB
        {
            public Guid ContextId { get; set; }
            public int IntProperty { get; set; }
            public string StringProperty { get; set; }
        }

        public interface IEventA : IEvent
        {
            Guid ContextId { get; set; }
            string StringProperty { get; set; }
        }

        public interface IEventB : IEvent
        {
            Guid ContextId { get; set; }
            int IntProperty { get; set; }
        }
    }
}