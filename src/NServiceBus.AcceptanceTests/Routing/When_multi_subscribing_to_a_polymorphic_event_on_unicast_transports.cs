namespace NServiceBus.AcceptanceTests.Routing
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_multi_subscribing_to_a_polymorphic_event_on_unicast_transports : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Both_events_should_be_delivered()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<Publisher1>(b => b.When(c => c.Publisher1HasASubscriberForIMyEvent, (session, c) =>
                {
                    c.AddTrace("Publishing MyEvent1");
                    return session.Publish(new MyEvent1());
                }))
                .WithEndpoint<Publisher2>(b => b.When(c => c.Publisher2HasDetectedASubscriberForEvent2, (session, c) =>
                {
                    c.AddTrace("Publishing MyEvent2");
                    return session.Publish(new MyEvent2());
                }))
                .WithEndpoint<Subscriber1>(b => b.When(async (session, c) =>
                {
                    c.AddTrace("Subscriber1 subscribing to both events");
                    await session.Subscribe<IMyEvent>();
                    await session.Subscribe<MyEvent2>();
                }))
                .Done(c => c.SubscriberGotIMyEvent && c.SubscriberGotMyEvent2)
                .Repeat(r => r.For<AllTransportsWithMessageDrivenPubSub>())
                .Should(c =>
                {
                    Assert.True(c.SubscriberGotIMyEvent);
                    Assert.True(c.SubscriberGotMyEvent2);
                })
                .Run();
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
                EndpointSetup<DefaultPublisher>(b =>
                {
                    //FLR on since subscription storages can throw on concurrency violation and need to retry
                    b.Recoverability().Immediate(immediate => immediate.NumberOfRetries(5));
                    b.OnEndpointSubscribed<Context>((args, context) =>
                    {
                        context.AddTrace("Publisher1 OnEndpointSubscribed " + args.MessageType);
                        if (args.MessageType.Contains(typeof(IMyEvent).Name))
                        {
                            context.Publisher1HasASubscriberForIMyEvent = true;
                        }
                    });
                });
            }
        }

        public class Publisher2 : EndpointConfigurationBuilder
        {
            public Publisher2()
            {
                EndpointSetup<DefaultPublisher>(b =>
                {
                    //FLR on since subscription storages can throw on concurrency violation and need to retry
                    b.Recoverability().Immediate(immediate => immediate.NumberOfRetries(5));

                    b.OnEndpointSubscribed<Context>((args, context) =>
                    {
                        context.AddTrace("Publisher2 OnEndpointSubscribed " + args.MessageType);

                        if (args.MessageType.Contains(typeof(MyEvent2).Name))
                        {
                            context.Publisher2HasDetectedASubscriberForEvent2 = true;
                        }
                    });
                });
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

        public class MyEvent1 : IMyEvent
        {
        }

        public class MyEvent2 : IMyEvent
        {
        }

        public interface IMyEvent : IEvent
        {
        }
    }
}