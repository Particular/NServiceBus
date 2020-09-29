namespace NServiceBus.AcceptanceTests.Core.Persistence
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Extensibility;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus.Persistence;
    using NUnit.Framework;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    public class When_a_persistence_does_not_provide_ISynchronizationContext : NServiceBusAcceptanceTest
    {
        // Run this test twice to ensure that the NoOpCompletableSynchronizedStorageSession's IDisposable method
        // is not altered by Fody to throw an ObjectDisposedException if it was disposed
        [Test]
        [Repeat(2)]
        public Task ReceiveFeature_should_work_without_ISynchronizedStorage()
        {
            return Scenario.Define<Context>()
                .WithEndpoint<NoSyncEndpoint>(e => e.When(b => b.SendLocal(new MyMessage())))
                .Done(c => c.MessageReceived)
                .Run();
        }

        class InMemoryNoSyncContextPersistence : PersistenceDefinition
        {
            public InMemoryNoSyncContextPersistence()
            {
                Supports<StorageType.Timeouts>(s => { });
                Supports<StorageType.Sagas>(s => { });
                Supports<StorageType.Subscriptions>(s => { });
            }
        }

        class NoOpISubscriptionStorage : ISubscriptionStorage
        {
            public Task<IEnumerable<Subscriber>> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes, ContextBag context)
            {
                return Task.FromResult<IEnumerable<Subscriber>>(null);
            }

            public Task Subscribe(Subscriber subscriber, MessageType messageType, ContextBag context)
            {
                return Task.FromResult(0);
            }

            public Task Unsubscribe(Subscriber subscriber, MessageType messageType, ContextBag context)
            {
                return Task.FromResult(0);
            }
        }

        class NoSyncEndpoint : EndpointConfigurationBuilder
        {
            public NoSyncEndpoint()
            {
                EndpointSetup<ServerWithNoDefaultPersistenceDefinitions>(c =>
                {
                    c.RegisterComponents(container => container.AddSingleton<ISubscriptionStorage, NoOpISubscriptionStorage>());
                    c.UsePersistence<InMemoryNoSyncContextPersistence>();
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