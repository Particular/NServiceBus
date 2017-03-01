namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using Outbox;
    using Persistence;
    using Pipeline;
    using Transport;

    class SynchronizedSessionConnector : StageConnector<IIncomingLogicalMessageContext, IUnitOfWorkContext>
    {
        public SynchronizedSessionConnector(ISynchronizedStorage synchronizedStorage, ISynchronizedStorageAdapter adapter)
        {
            this.adapter = adapter;
            this.synchronizedStorage = synchronizedStorage;
        }

        public override async Task Invoke(IIncomingLogicalMessageContext context, Func<IUnitOfWorkContext, Task> stage)
        {
            var outboxTransaction = context.Extensions.Get<OutboxTransaction>();
            var transportTransaction = context.Extensions.Get<TransportTransaction>();
            using (var storageSession = await AdaptOrOpenNewSynchronizedStorageSession(transportTransaction, outboxTransaction, context.Extensions).ConfigureAwait(false))
            {
                var uowContext = this.CreateUnitOfWorkContext(storageSession, context);

                await stage(uowContext).ConfigureAwait(false);
                context.MessageHandled = uowContext.MessageHandled;

                await storageSession.CompleteAsync().ConfigureAwait(false);
            }
        }

        async Task<CompletableSynchronizedStorageSession> AdaptOrOpenNewSynchronizedStorageSession(TransportTransaction transportTransaction, OutboxTransaction outboxTransaction, ContextBag contextBag)
        {
            return await adapter.TryAdapt(outboxTransaction, contextBag).ConfigureAwait(false)
                   ?? await adapter.TryAdapt(transportTransaction, contextBag).ConfigureAwait(false)
                   ?? await synchronizedStorage.OpenSession(contextBag).ConfigureAwait(false);
        }

        ISynchronizedStorageAdapter adapter;
        ISynchronizedStorage synchronizedStorage;
    }
}