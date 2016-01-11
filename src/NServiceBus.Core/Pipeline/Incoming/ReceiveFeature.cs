namespace NServiceBus.Features
{
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Outbox;
    using NServiceBus.Persistence;
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;
    using NServiceBus.Unicast;

    class ReceiveFeature : Feature
    {
        public ReceiveFeature()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Pipeline.RegisterConnector<TransportReceiveToPhysicalMessageProcessingConnector>("Allows to abort processing the message");
            context.Pipeline.RegisterConnector<LoadHandlersConnector>("Gets all the handlers to invoke from the MessageHandler registry based on the message type.");


            context.Pipeline
                .Register(WellKnownStep.ExecuteUnitOfWork, typeof(UnitOfWorkBehavior), "Executes the UoW")
                .Register(WellKnownStep.MutateIncomingTransportMessage, typeof(MutateIncomingTransportMessageBehavior), "Executes IMutateIncomingTransportMessages")
                .Register(WellKnownStep.MutateIncomingMessages, typeof(MutateIncomingMessageBehavior), "Executes IMutateIncomingMessages")
                .Register(WellKnownStep.InvokeHandlers, typeof(InvokeHandlerTerminator), "Calls the IHandleMessages<T>.Handle(T)");

            context.Container.ConfigureComponent(b =>
            {
                var storage = context.Container.HasComponent<IOutboxStorage>() ? b.Build<IOutboxStorage>() : new NoOpOutbox();
                
                return new TransportReceiveToPhysicalMessageProcessingConnector(storage);
            }, DependencyLifecycle.InstancePerCall);

            context.Container.ConfigureComponent(b =>
            {
                var adapter = context.Container.HasComponent<ISynchronizedStorageAdapter>() ? b.Build<ISynchronizedStorageAdapter>() : new NoOpAdaper();
                return new LoadHandlersConnector(b.Build<MessageHandlerRegistry>(), b.Build<ISynchronizedStorage>(), adapter);
            }, DependencyLifecycle.InstancePerCall);
        }

        class NoOpAdaper : ISynchronizedStorageAdapter
        {
            static readonly Task<CompletableSynchronizedStorageSession> EmptyResult = Task.FromResult<CompletableSynchronizedStorageSession>(null);

            public Task<CompletableSynchronizedStorageSession> TryAdapt(OutboxTransaction transaction, ContextBag context)
            {
                return EmptyResult;
            }

            public Task<CompletableSynchronizedStorageSession> TryAdapt(TransportTransaction transportTransaction, ContextBag context)
            {
                return EmptyResult;
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