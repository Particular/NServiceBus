namespace NServiceBus.AcceptanceTests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Extensibility;
    using Features;
    using NServiceBus;
    using NUnit.Framework;
    using Persistence;
    using ScenarioDescriptors;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    public class When_publishing_from_sendonly : NServiceBusAcceptanceTest
    {
        [Test]
        public Task Should_be_delivered_to_all_subscribers()
        {
            return Scenario.Define<Context>()
                .WithEndpoint<SendOnlyPublisher>(b => b.When((session, c) => session.Publish(new MyEvent())))
                .WithEndpoint<Subscriber>()
                .Done(c => c.SubscriberGotTheEvent)
                .Repeat(r => r.For<AllTransportsWithMessageDrivenPubSub>())
                .Should(ctx => Assert.True(ctx.SubscriberGotTheEvent))
                .Run();
        }

        public class Context : ScenarioContext
        {
            public bool SubscriberGotTheEvent { get; set; }
        }

        public class SendOnlyPublisher : EndpointConfigurationBuilder
        {
            public SendOnlyPublisher()
            {
                EndpointSetup<DefaultPublisher>(b =>
                {
                    b.UsePersistence(typeof(HardCodedPersistence));
                    b.DisableFeature<AutoSubscribe>();
                }).SendOnly();
            }
        }

        public class Subscriber : EndpointConfigurationBuilder
        {
            public Subscriber()
            {
                EndpointSetup<DefaultServer>(c => c.DisableFeature<AutoSubscribe>())
                    .AddMapping<MyEvent>(typeof(SendOnlyPublisher));
            }

            public class MyEventHandler : IHandleMessages<MyEvent>
            {
                public Context Context { get; set; }

                public Task Handle(MyEvent messageThatIsEnlisted, IMessageHandlerContext context)
                {
                    Context.SubscriberGotTheEvent = true;

                    return Task.FromResult(0);
                }
            }
        }

        [Serializable]
        public class MyEvent : IEvent
        {
        }

        public class HardCodedPersistence : PersistenceDefinition
        {
            internal HardCodedPersistence()
            {
                Supports<StorageType.Subscriptions>(s => s.EnableFeatureByDefault<HardCodedPersistenceFeature>());
            }
        }

        public class HardCodedPersistenceFeature : Feature
        {
            protected override void Setup(FeatureConfigurationContext context)
            {
                context.Container.ConfigureComponent<HardcodedSubscriptionManager>(DependencyLifecycle.SingleInstance);
            }
        }

        public class HardcodedSubscriptionManager : ISubscriptionStorage
        {
            public HardcodedSubscriptionManager()
            {
                addressTask = Task.FromResult(new[]
                {
                    new Unicast.Subscriptions.MessageDrivenSubscriptions.Subscriber("publishingFromSendonly.subscriber", null)
                }.AsEnumerable());
            }

            public Task Subscribe(Unicast.Subscriptions.MessageDrivenSubscriptions.Subscriber subscriber, MessageType messageType, ContextBag context)
            {
                return Task.FromResult(0);
            }

            public Task Unsubscribe(Unicast.Subscriptions.MessageDrivenSubscriptions.Subscriber subscriber, MessageType messageType, ContextBag context)
            {
                return Task.FromResult(0);
            }

            public Task<IEnumerable<Unicast.Subscriptions.MessageDrivenSubscriptions.Subscriber>> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes, ContextBag context)
            {
                return addressTask;
            }

            Task<IEnumerable<Unicast.Subscriptions.MessageDrivenSubscriptions.Subscriber>> addressTask;
        }
    }
}