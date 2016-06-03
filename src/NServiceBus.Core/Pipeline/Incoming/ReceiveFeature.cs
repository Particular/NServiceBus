namespace NServiceBus.Features
{
    using System.Threading.Tasks;
    using Extensibility;
    using Janitor;
    using NServiceBus.Outbox;
    using Persistence;
    using Transports;
    using Unicast;

    class ReceiveFeature : Feature
    {
        public ReceiveFeature()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Pipeline.Register(typeof(TransportReceiveToPhysicalMessageProcessingConnector), "Allows to abort processing the message");
            context.Pipeline.Register(typeof(LoadHandlersConnector), "Gets all the handlers to invoke from the MessageHandler registry based on the message type.");

            context.Pipeline
                .Register("ExecuteUnitOfWork", typeof(UnitOfWorkBehavior), "Executes the UoW")
                .Register("MutateIncomingTransportMessage", typeof(MutateIncomingTransportMessageBehavior), "Executes IMutateIncomingTransportMessages")
                .Register("MutateIncomingMessages", typeof(MutateIncomingMessageBehavior), "Executes IMutateIncomingMessages")
                .Register("InvokeHandlers", typeof(InvokeHandlerTerminator), "Calls the IHandleMessages<T>.Handle(T)");

            context.Container.ConfigureComponent(b =>
            {
                var storage = context.Container.HasComponent<IOutboxStorage>() ? b.Build<IOutboxStorage>() : new NoOpOutbox();

                return new TransportReceiveToPhysicalMessageProcessingConnector(storage);
            }, DependencyLifecycle.InstancePerCall);

            context.Container.ConfigureComponent(b =>
            {
                var adapter = context.Container.HasComponent<ISynchronizedStorageAdapter>() ? b.Build<ISynchronizedStorageAdapter>() : new NoOpAdaper();
                var syncStorage = context.Container.HasComponent<ISynchronizedStorage>() ? b.Build<ISynchronizedStorage>() : new NoOpSynchronizedStorage();

                return new LoadHandlersConnector(b.Build<MessageHandlerRegistry>(), syncStorage, adapter);
            }, DependencyLifecycle.InstancePerCall);
        }

        class NoOpSynchronizedStorage : ISynchronizedStorage
        {
            public Task<CompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag)
            {
                return NoOpAdaper.EmptyResult;
            }
        }

        class NoOpAdaper : ISynchronizedStorageAdapter
        {
            public Task<CompletableSynchronizedStorageSession> TryAdapt(OutboxTransaction transaction, ContextBag context)
            {
                return EmptyResult;
            }

            public Task<CompletableSynchronizedStorageSession> TryAdapt(TransportTransaction transportTransaction, ContextBag context)
            {
                return EmptyResult;
            }

            internal static readonly Task<CompletableSynchronizedStorageSession> EmptyResult = Task.FromResult<CompletableSynchronizedStorageSession>(new NoOpCompletableSynchronizedStorageSession());
        }

        // Do not allow Fody to weave the IDisposable for us so that other threads can still access the instance of this class
        // even after it has been disposed.
        [SkipWeaving]
        class NoOpCompletableSynchronizedStorageSession : CompletableSynchronizedStorageSession
        {
            public Task CompleteAsync()
            {
                return TaskEx.CompletedTask;
            }

            public void Dispose()
            {
            }
        }

        class NoOpOutbox : IOutboxStorage
        {
            public Task<OutboxMessage> Get(string messageId, ContextBag options)
            {
                return NoOutboxMessageTask;
            }

            public Task Store(OutboxMessage message, OutboxTransaction transaction, ContextBag options)
            {
                return TaskEx.CompletedTask;
            }

            public Task SetAsDispatched(string messageId, ContextBag options)
            {
                return TaskEx.CompletedTask;
            }

            public Task<OutboxTransaction> BeginTransaction(ContextBag context)
            {
                return Task.FromResult<OutboxTransaction>(new NoOpOutboxTransaction());
            }

            static Task<OutboxMessage> NoOutboxMessageTask = Task.FromResult<OutboxMessage>(null);
        }

        class NoOpOutboxTransaction : OutboxTransaction
        {
            public void Dispose()
            {
            }

            public Task Commit()
            {
                return TaskEx.CompletedTask;
            }
        }
    }
}