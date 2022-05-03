namespace NServiceBus.AcceptanceTests.Core.Persistence
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Extensibility;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus.Persistence;
    using NUnit.Framework;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    public class When_a_persistence_does_not_provide_synchronized_storage_session : NServiceBusAcceptanceTest
    {
        // Run this test twice to ensure that the NoOpCompletableSynchronizedStorageSession's IDisposable method
        // is not altered by Fody to throw an ObjectDisposedException if it was disposed
        [Test]
        [Repeat(2)]
        public async Task ReceiveFeature_should_work_without_ISynchronizedStorage()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<NoSyncEndpoint>(e => e.When(b => b.SendLocal(new MyMessage())))
                .Done(c => c.MessageReceived)
                .Run();
        }

        class FakeNoSynchronizedStorageSupportPersistence : PersistenceDefinition
        {
            public FakeNoSynchronizedStorageSupportPersistence()
            {
                Supports<StorageType.Sagas>(s => { });
                Supports<StorageType.Subscriptions>(s => { });
            }
        }

        class NoOpISubscriptionStorage : ISubscriptionStorage
        {
            public Task<IEnumerable<Subscriber>> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes, ContextBag context, CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IEnumerable<Subscriber>>(null);
            }

            public Task Subscribe(Subscriber subscriber, MessageType messageType, ContextBag context, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task Unsubscribe(Subscriber subscriber, MessageType messageType, ContextBag context, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }

        class NoSyncEndpoint : EndpointConfigurationBuilder
        {
            public NoSyncEndpoint()
            {
                EndpointSetup<ServerWithNoDefaultPersistenceDefinitions>(c =>
                {
                    // The subscription storage is needed because at this stage we have no way of DisablingPublishing on the non-generic version of ConfigureTransport
                    c.RegisterComponents(container => container.AddSingleton<ISubscriptionStorage, NoOpISubscriptionStorage>());
                    c.UsePersistence<FakeNoSynchronizedStorageSupportPersistence>();
                });
            }
        }

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            public MyMessageHandler(Context context)
            {
                testContext = context;
            }

            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                testContext.MessageReceived = true;

                return Task.FromResult(0);
            }

            Context testContext;
        }

        public class Context : ScenarioContext
        {
            public bool NotSet { get; set; }
            public bool MessageReceived { get; set; }
        }

        public class MyMessage : ICommand
        {
        }
    }
}