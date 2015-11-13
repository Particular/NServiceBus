namespace NServiceBus.AcceptanceTests.Routing
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class When_multi_subscribing_to_a_polymorphic_event : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Both_events_should_be_delivered()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Publisher1>(b => b.When(c => c.Publisher1HasASubscriberForIMyEvent, (bus, c) =>
                {
                    c.AddTrace("Publishing MyEvent1");
                    return bus.Publish(new MyEvent1());
                }))
                .WithEndpoint<Publisher2>(b => b.When(c => c.Publisher2HasDetectedASubscriberForEvent2, (bus, c) =>
                {
                    c.AddTrace("Publishing MyEvent2");
                    return bus.Publish(new MyEvent2());
                }))
                .WithEndpoint<Subscriber1>(b => b.When(async (bus, c) =>
                {
                    c.AddTrace("Subscriber1 subscribing to both events");
                    await bus.Subscribe<IMyEvent>();
                    await bus.Subscribe<MyEvent2>();

                    if (c.HasNativePubSubSupport)
                    {
                        c.Publisher1HasASubscriberForIMyEvent = true;
                        c.Publisher2HasDetectedASubscriberForEvent2 = true;
                    }
                }))
                .Done(c => c.SubscriberGotIMyEvent && c.SubscriberGotMyEvent2)
                .Run();

            Assert.True(context.SubscriberGotIMyEvent);
            Assert.True(context.SubscriberGotMyEvent2);
        }

        public class Context : ScenarioContext
        {
            public bool SubscriberGotIMyEvent { get; set; }
            public bool SubscriberGotMyEvent2 { get; set; }
            public bool Publisher1HasASubscriberForIMyEvent { get; set; }
            public bool Publisher2HasDetectedASubscriberForEvent2 { get; set; }
        }

        public class Publisher1 : EndpointConfigurationBuilder
        {
            public Publisher1()
            {
                EndpointSetup<DefaultPublisher>(b => b.OnEndpointSubscribed<Context>((args, context) =>
               {
                   context.AddTrace("Publisher1 OnEndpointSubscribed " + args.MessageType);
                   if (args.MessageType.Contains(typeof(IMyEvent).Name))
                   {
                       context.Publisher1HasASubscriberForIMyEvent = true;
                   }
               }));
            }
        }

        public class Publisher2 : EndpointConfigurationBuilder
        {
            public Publisher2()
            {
                EndpointSetup<DefaultPublisher>(b => b.OnEndpointSubscribed<Context>((args, context) =>
                {
                    context.AddTrace("Publisher2 OnEndpointSubscribed " + args.MessageType);

                    if (args.MessageType.Contains(typeof(MyEvent2).Name))
                    {
                        context.Publisher2HasDetectedASubscriberForEvent2 = true;
                    }
                }));
            }
        }

        public class Subscriber1 : EndpointConfigurationBuilder
        {
            public Subscriber1()
            {
                EndpointSetup<DefaultServer>(c => c.DisableFeature<AutoSubscribe>())
                    .AddMapping<IMyEvent>(typeof(Publisher1))
                    .AddMapping<MyEvent2>(typeof(Publisher2));
            }

            public class MyEventHandler : IHandleMessages<IMyEvent>
            {
                public Context Context { get; set; }

                public Task Handle(IMyEvent messageThatIsEnlisted, IMessageHandlerContext context)
                {
                    Context.AddTrace($"Got event '{messageThatIsEnlisted}'");
                    if (messageThatIsEnlisted is MyEvent2)
                    {
                        Context.SubscriberGotMyEvent2 = true;
                    }
                    else
                    {
                        Context.SubscriberGotIMyEvent = true;
                    }

                    return Task.FromResult(0);
                }
            }
        }

        [Serializable]
        public class MyEvent1 : IMyEvent
        {
        }

        [Serializable]
        public class MyEvent2 : IMyEvent
        {
        }

        public interface IMyEvent : IEvent
        {
        }
    }
}
