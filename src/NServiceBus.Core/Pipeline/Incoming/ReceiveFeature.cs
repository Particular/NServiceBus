﻿namespace NServiceBus.Features
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
            context.Pipeline.RegisterConnector<DeserializeLogicalMessagesConnector>("Deserializes the physical message body into logical messages");
            context.Pipeline.RegisterConnector<LoadHandlersConnector>("Gets all the handlers to invoke from the MessageHandler registry based on the message type.");


            context.Pipeline
                .Register(WellKnownStep.ExecuteUnitOfWork, typeof(UnitOfWorkBehavior), "Executes the UoW")
                .Register(WellKnownStep.MutateIncomingTransportMessage, typeof(MutateIncomingTransportMessageBehavior), "Executes IMutateIncomingTransportMessages")
                .Register(WellKnownStep.MutateIncomingMessages, typeof(MutateIncomingMessageBehavior), "Executes IMutateIncomingMessages")
                .Register(WellKnownStep.InvokeHandlers, typeof(InvokeHandlerTerminator), "Calls the IHandleMessages<T>.Handle(T)");

            context.Container.ConfigureComponent(b =>
            {
                var pipelinesCollection = context.Settings.Get<PipelineConfiguration>();

                var pipeline = new PipelineBase<BatchDispatchContext>(b, context.Settings, pipelinesCollection.MainPipeline);

                var storage = context.Container.HasComponent<IOutboxStorage>() ? b.Build<IOutboxStorage>() : new NoOpOutbox();
                
                return new TransportReceiveToPhysicalMessageProcessingConnector(pipeline, storage);
            }, DependencyLifecycle.InstancePerCall);

            context.Container.ConfigureComponent(b =>
            {
                var adapter = context.Container.HasComponent<ISynchronizedStorageAdapter>() ? b.Build<ISynchronizedStorageAdapter>() : new NoOpAdaper();
                return new LoadHandlersConnector(b.Build<MessageHandlerRegistry>(), b.Build<ISynchronizedStorage>(), adapter);
            }, DependencyLifecycle.InstancePerCall);
        }

        class NoOpAdaper : ISynchronizedStorageAdapter
        {
            public bool TryAdapt(OutboxTransaction transaction, out ICompletableSynchronizedStorageSession session)
            {
                session = null;
                return false;
            }

            public bool TryAdapt(TransportTransaction transportTransaction, out ICompletableSynchronizedStorageSession session)
            {
                session = null;
                return false;
            }
        }

        class NoOpOutbox : IOutboxStorage
        {
            public Task<OutboxMessage> Get(string messageId, ContextBag options)
            {
                return Task.FromResult<OutboxMessage>(null);
            }

            public Task Store(OutboxMessage message, OutboxTransaction transaction, ContextBag options)
            {
                return TaskEx.Completed;
            }

            public Task SetAsDispatched(string messageId, ContextBag options)
            {
                return TaskEx.Completed;
            }

            public Task<OutboxTransaction> BeginTransaction(ContextBag context)
            {
                return Task.FromResult<OutboxTransaction>(new NoOpOutboxTransaction());
            }
        }
        class NoOpOutboxTransaction : OutboxTransaction
        {
            public void Dispose()
            {
            }

            public Task Commit()
            {
                return TaskEx.Completed;
            }
        }
    }
}