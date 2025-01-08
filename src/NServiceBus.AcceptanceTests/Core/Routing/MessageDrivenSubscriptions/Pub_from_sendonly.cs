namespace NServiceBus.AcceptanceTests.Core.Routing.MessageDrivenSubscriptions;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NServiceBus.Extensibility;
using NServiceBus.Features;
using NServiceBus.Persistence;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;
using NUnit.Framework;
using Conventions = AcceptanceTesting.Customization.Conventions;

public class Pub_from_sendonly : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_be_delivered_to_all_subscribers()
    {
        Requires.MessageDrivenPubSub();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<SendOnlyPublisher>(b => b.When((session, c) => session.Publish(new MyEvent())))
            .WithEndpoint<Subscriber>()
            .Done(c => c.SubscriberGotTheEvent)
            .Run();

        Assert.That(context.SubscriberGotTheEvent, Is.True);
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
                b.SendOnly();
                b.UsePersistence(typeof(HardCodedPersistence));
                b.DisableFeature<AutoSubscribe>();
            }, metadata => metadata.RegisterSelfAsPublisherFor<MyEvent>(this));
        }
    }

    public class Subscriber : EndpointConfigurationBuilder
    {
        public Subscriber()
        {
            EndpointSetup<DefaultServer>(c => c.DisableFeature<AutoSubscribe>());
        }

        public class MyHandler : IHandleMessages<MyEvent>
        {
            public MyHandler(Context context)
            {
                testContext = context;
            }

            public Task Handle(MyEvent messageThatIsEnlisted, IMessageHandlerContext context)
            {
                testContext.SubscriberGotTheEvent = true;

                return Task.CompletedTask;
            }

            Context testContext;
        }
    }


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
            context.Services.AddSingleton(typeof(ISubscriptionStorage), typeof(HardcodedSubscriptionManager));
        }
    }

    public class HardcodedSubscriptionManager : ISubscriptionStorage
    {
        public HardcodedSubscriptionManager()
        {
            addressTask = Task.FromResult(new[]
            {
                new Unicast.Subscriptions.MessageDrivenSubscriptions.Subscriber(Conventions.EndpointNamingConvention(typeof(Subscriber)), null)
            }.AsEnumerable());
        }

        public Task Subscribe(Unicast.Subscriptions.MessageDrivenSubscriptions.Subscriber subscriber, MessageType messageType, ContextBag context, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task Unsubscribe(Unicast.Subscriptions.MessageDrivenSubscriptions.Subscriber subscriber, MessageType messageType, ContextBag context, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<IEnumerable<Unicast.Subscriptions.MessageDrivenSubscriptions.Subscriber>> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes, ContextBag context, CancellationToken cancellationToken = default)
        {
            return addressTask;
        }

        Task<IEnumerable<Unicast.Subscriptions.MessageDrivenSubscriptions.Subscriber>> addressTask;
    }
}