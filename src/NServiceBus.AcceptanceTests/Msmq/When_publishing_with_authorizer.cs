namespace NServiceBus.AcceptanceTests.Msmq
{
    using System.Collections.Generic;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NServiceBus.Features;
    using NServiceBus.MessageMutator;
    using NServiceBus.Persistence.InMemory;
    using NUnit.Framework;

    public class When_publishing_with_authorizer : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_only_deliver_to_authorized()
        {
            Scenario.Define<TestContext>()
                .WithEndpoint<Publisher>(b =>
                    b.When(c => c.Subscriber1Subscribed && c.Subscriber2Subscribed, (bus, c) => bus.Publish(new MyEvent()))
                )
                .WithEndpoint<Subscriber1>(b => b.When(bus =>
                {
                    bus.Subscribe<MyEvent>();
                }))
                .WithEndpoint<Subscriber2>(b => b.When(bus =>
                {
                    bus.Subscribe<MyEvent>();
                }))
                .Done(c =>
                    c.Subscriber1GotTheEvent &&
                    c.DeclinedSubscriber2)
                .Repeat(r => r.For(Transports.Msmq))
                .Should(c =>
                {
                    Assert.True(c.Subscriber1GotTheEvent);
                    Assert.False(c.Subscriber2GotTheEvent);
                })
                .Run();
        }

        public class TestContext : ScenarioContext
        {
            public bool Subscriber1GotTheEvent { get; set; }
            public bool Subscriber2GotTheEvent { get; set; }
            public bool Subscriber1Subscribed { get; set; }
            public bool Subscriber2Subscribed { get; set; }
            public bool DeclinedSubscriber2 { get; set; }
        }

        class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultServer>(b =>
                {
                 //   InMemoryPersistence.UseAsDefault();
                    Configure.Features.Disable<AutoSubscribe>();
                    Configure.Component<MyTransportMessageMutator>(DependencyLifecycle.InstancePerCall);
                });
            }

            class MyTransportMessageMutator : IMutateIncomingTransportMessages
            {
                TestContext context;

                public MyTransportMessageMutator(TestContext context)
                {
                    this.context = context;
                }

                public void MutateIncoming(TransportMessage transportMessage)
                {
                    if (transportMessage.MessageIntent == MessageIntentEnum.Subscribe)
                    {
                        var originatingEndpoint = transportMessage.Headers[Headers.OriginatingEndpoint];
                        if (originatingEndpoint.Contains("Subscriber1"))
                        {
                            context.Subscriber1Subscribed = true;
                        }
                        if (originatingEndpoint.Contains("Subscriber2"))
                        {
                            context.Subscriber2Subscribed = true;
                        }
                    }
                }
            }

            public class SubscriptionAuthorizer : IAuthorizeSubscriptions
            {
                TestContext context;

                public SubscriptionAuthorizer(TestContext context)
                {
                    this.context = context;
                }

                public bool AuthorizeSubscribe(string messageType, string clientEndpoint, IDictionary<string, string> headers)
                {
                    var isFromSubscriber1 = headers[Headers.OriginatingEndpoint]
                        .Contains("Subscriber1");
                    if (!isFromSubscriber1)
                    {
                        context.DeclinedSubscriber2 = true;
                    }
                    return isFromSubscriber1;
                }

                public bool AuthorizeUnsubscribe(string messageType, string clientEndpoint, IDictionary<string, string> headers)
                {
                    return true;
                }
            }
        }

        public class Subscriber1 : EndpointConfigurationBuilder
        {
            public Subscriber1()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    InMemoryPersistence.UseAsDefault();
                    Configure.Features.Disable<AutoSubscribe>();
                })
                    .AddMapping<MyEvent>(typeof(Publisher));
            }

            public class MyEventHandler : IHandleMessages<MyEvent>
            {
                TestContext context;

                public MyEventHandler(TestContext context)
                {
                    this.context = context;
                }

                public void Handle(MyEvent message)
                {
                    context.Subscriber1GotTheEvent = true;
                }
            }


        }

        public class Subscriber2 : EndpointConfigurationBuilder
        {
            public Subscriber2()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    InMemoryPersistence.UseAsDefault();
                    Configure.Features.Disable<AutoSubscribe>();
                })
                    .AddMapping<MyEvent>(typeof(Publisher));
            }

            public class MyEventHandler : IHandleMessages<MyEvent>
            {
                TestContext context;

                public MyEventHandler(TestContext context)
                {
                    this.context = context;
                }

                public void Handle(MyEvent messageThatIsEnlisted)
                {
                    context.Subscriber2GotTheEvent = true;
                }
            }
        }

        public class MyEvent : IEvent
        {
        }
    }
}