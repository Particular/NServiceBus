namespace NServiceBus.AcceptanceTests.PublishSubscribe
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Configuration.AdvancedExtensibility;
    using EndpointTemplates;
    using Extensibility;
    using Features;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using Persistence;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    public class When_configuring_subscription_authorizer : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_apply_authorizer_on_subscriptions()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Subscriber>(e => e
                    .When(async s =>
                    {
                        await s.Subscribe<ForbiddenEvent>();
                        await s.Subscribe<AllowedEvent>();
                    }))
                .WithEndpoint<PublisherWithAuthorizer>()
                .Done(c => c.ReceivedAllowedEventSubscriptionMessage && c.ReceivedForbiddenEventSubscriptionMessage)
                .Run();

            Assert.AreEqual(1, context.SubscriptionStorage.SubscribedEvents.Count);
            Assert.AreEqual(typeof(AllowedEvent).FullName, context.SubscriptionStorage.SubscribedEvents.Single());
        }

        class Context : ScenarioContext
        {
            public bool ReceivedAllowedEventSubscriptionMessage { get; set; }
            public bool ReceivedForbiddenEventSubscriptionMessage { get; set; }
            public FakePersistence.FakeSubscriptionStorage SubscriptionStorage { get; set; }
        }

        class Subscriber : EndpointConfigurationBuilder
        {
            public Subscriber()
            {
                var defaultServer = new DefaultServer
                {
                    TransportConfiguration = new ConfigureEndpointAcceptanceTestingTransport(false, false)
                };
                EndpointSetup(defaultServer, (_, __) => { }, p =>
                {
                    p.RegisterPublisherFor<ForbiddenEvent>(typeof(PublisherWithAuthorizer));
                    p.RegisterPublisherFor<AllowedEvent>(typeof(PublisherWithAuthorizer));
                });
            }
        }

        class PublisherWithAuthorizer : EndpointConfigurationBuilder
        {
            public PublisherWithAuthorizer()
            {
                var defaultServer = new DefaultServer
                {
                    TransportConfiguration = new ConfigureEndpointAcceptanceTestingTransport(false, false),
                };
                EndpointSetup(defaultServer, (endpointConfiguration, descriptor) =>
                {
                    endpointConfiguration.UsePersistence<FakePersistence>();
                    endpointConfiguration.EnableFeature<StorageAccessorFeature>();
                    endpointConfiguration.OnEndpointSubscribed<Context>((args, ctx) =>
                    {
                        if (args.MessageType.Contains(typeof(AllowedEvent).FullName))
                        {
                            ctx.ReceivedAllowedEventSubscriptionMessage = true;
                        }
                        if (args.MessageType.Contains(typeof(ForbiddenEvent).FullName))
                        {
                            ctx.ReceivedForbiddenEventSubscriptionMessage = true;
                        }
                    });

                    var routingSettings =
                        new RoutingSettings<AcceptanceTestingTransport>(endpointConfiguration.GetSettings());
                    routingSettings.SubscriptionAuthorizer(ctx =>
                    {
                        // only allow subscriptions for AllowedEvent:
                        return ctx.MessageHeaders[Headers.SubscriptionMessageType]
                            .Contains(typeof(AllowedEvent).FullName);
                    });
                });
            }

            class StorageAccessorFeature : Feature
            {
                protected override void Setup(FeatureConfigurationContext context) =>
                    context.RegisterStartupTask(
                        sp => new StorageAccessor(sp.GetRequiredService<ISubscriptionStorage>(),
                            sp.GetRequiredService<Context>()));

                class StorageAccessor : FeatureStartupTask
                {
                    public StorageAccessor(ISubscriptionStorage subscriptionStorage, Context testContext)
                    {
                        testContext.SubscriptionStorage = (FakePersistence.FakeSubscriptionStorage)subscriptionStorage;
                    }

                    protected override Task OnStart(IMessageSession session) => Task.CompletedTask;

                    protected override Task OnStop(IMessageSession session) => Task.CompletedTask;
                }
            }
        }

        public class AllowedEvent : IEvent
        {
        }

        public class ForbiddenEvent : IEvent
        {
        }

        class FakePersistence : PersistenceDefinition
        {
            public FakePersistence()
            {
                Supports<StorageType.Subscriptions>(s => s.EnableFeatureByDefault(typeof(SubscriptionStorageFeature)));
            }

            class SubscriptionStorageFeature : Feature
            {
                protected override void Setup(FeatureConfigurationContext context)
                {
                    context.Services.AddSingleton<ISubscriptionStorage>(new FakeSubscriptionStorage());
                }
            }

            public class FakeSubscriptionStorage : ISubscriptionStorage
            {
                public ConcurrentBag<string> SubscribedEvents { get; } = new ConcurrentBag<string>();

                public Task Subscribe(Unicast.Subscriptions.MessageDrivenSubscriptions.Subscriber subscriber, MessageType messageType, ContextBag context)
                {
                    SubscribedEvents.Add(messageType.TypeName);
                    return Task.CompletedTask;
                }

                public Task Unsubscribe(Unicast.Subscriptions.MessageDrivenSubscriptions.Subscriber subscriber, MessageType messageType, ContextBag context) => throw new NotImplementedException();

                public Task<IEnumerable<Unicast.Subscriptions.MessageDrivenSubscriptions.Subscriber>> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes, ContextBag context) => throw new NotImplementedException();
            }
        }
    }
}