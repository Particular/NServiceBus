namespace NServiceBus.AcceptanceTests.Core.Persistence;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Extensibility;
using Features;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Persistence;
using NUnit.Framework;
using Unicast.Subscriptions;
using Unicast.Subscriptions.MessageDrivenSubscriptions;

public class When_a_persistence_does_not_provide_synchronized_storage_session : NServiceBusAcceptanceTest
{
    [Test]
    public async Task ReceiveFeature_should_work_without_ISynchronizedStorage() =>
        await Scenario.Define<Context>()
            .WithEndpoint<NoSyncEndpoint>(e => e.When(b => b.SendLocal(new MyMessage())))
            .Run();

    class FakeNoSynchronizedStorageSupportPersistence : PersistenceDefinition, IPersistenceDefinitionFactory<FakeNoSynchronizedStorageSupportPersistence>
    {
        FakeNoSynchronizedStorageSupportPersistence()
        {
            Supports<StorageType.Sagas, FakeStorage>();
            Supports<StorageType.Subscriptions, FakeStorage>();
        }

        public static FakeNoSynchronizedStorageSupportPersistence Create() => new();

        sealed class FakeStorage : Feature
        {
            protected override void Setup(FeatureConfigurationContext context)
            {
            }
        }
    }

    class NoOpISubscriptionStorage : ISubscriptionStorage
    {
        public Task<IEnumerable<Subscriber>> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes, ContextBag context, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<Subscriber>>(null);

        public Task Subscribe(Subscriber subscriber, MessageType messageType, ContextBag context, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Unsubscribe(Subscriber subscriber, MessageType messageType, ContextBag context, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    public class NoSyncEndpoint : EndpointConfigurationBuilder
    {
        public NoSyncEndpoint() =>
            EndpointSetup<ServerWithNoDefaultPersistenceDefinitions>(c =>
            {
                // The subscription storage is needed because at this stage we have no way of DisablingPublishing on the non-generic version of ConfigureTransport
                c.RegisterComponents(container => container.AddSingleton<ISubscriptionStorage, NoOpISubscriptionStorage>());
                c.UsePersistence<FakeNoSynchronizedStorageSupportPersistence>();
            });

        [Handler]
        public class MyMessageHandler(Context testContext) : IHandleMessages<MyMessage>
        {
            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class Context : ScenarioContext;

    public class MyMessage : ICommand;
}