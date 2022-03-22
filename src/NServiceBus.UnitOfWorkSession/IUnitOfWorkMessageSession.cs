namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Outbox;
    using Persistence;
    using Routing;
    using Transport;
    using TransportOperation = Outbox.TransportOperation;

    // Maybe IAsyncDisposable
    public interface IUnitOfWorkMessageSession : IMessageSession, IDisposable
    {
        /// <summary>
        /// Gets the synchronized storage session for processing the current message. NServiceBus makes sure the changes made
        /// via this session will be persisted before the message receive is acknowledged.
        /// </summary>
        ISynchronizedStorageSession SynchronizedStorageSession { get; }

        string SessionId { get; }

        bool ShouldOnlyBeCommitted { get; }

        // Name super temporary
        Task Commit(CancellationToken cancellationToken = default);
    }

    class UnitOfWorkMessageSession : IUnitOfWorkMessageSession
    {
        readonly IMessageSession decoratedMessageSession;
        readonly PendingTransportOperations pendingTransportOperations;
        readonly IOutboxStorage outboxStorage;
        readonly IMessageDispatcher dispatcher;
        IOutboxTransaction? outboxTransaction;
        readonly ContextBag contextBag;
        ICompletableSynchronizedStorageSession? synchronizedStorageSession;
        readonly ISynchronizedStorageAdapter adapter;
        readonly ISynchronizedStorage synchronizedStorage;
        readonly TransportTransaction transportTransaction;

        public UnitOfWorkMessageSession(IMessageSession decoratedMessageSession, IMessageDispatcher dispatcher, IOutboxStorage outboxStorage, ISynchronizedStorageAdapter adapter, ISynchronizedStorage synchronizedStorage, string sessionId)
        {
            this.synchronizedStorage = synchronizedStorage;
            this.adapter = adapter;
            this.dispatcher = dispatcher;
            this.outboxStorage = outboxStorage;
            this.decoratedMessageSession = decoratedMessageSession;
            pendingTransportOperations = new PendingTransportOperations();
            contextBag = new ContextBag();
            transportTransaction = new TransportTransaction();
            SessionId = sessionId;
        }

        public string SessionId { get; }
        public bool ShouldOnlyBeCommitted { get; private set; }

        public ISynchronizedStorageSession SynchronizedStorageSession => synchronizedStorageSession ?? throw new InvalidOperationException("Not initialized");

        async Task<ICompletableSynchronizedStorageSession> AdaptOrOpenNewSynchronizedStorageSession(CancellationToken cancellationToken) =>
            await adapter.TryAdapt(outboxTransaction, contextBag, cancellationToken).ConfigureAwait(false)
            ?? await adapter.TryAdapt(transportTransaction, contextBag, cancellationToken).ConfigureAwait(false)
            ?? await synchronizedStorage.OpenSession(contextBag, cancellationToken).ConfigureAwait(false);

        public async Task Send(object message, SendOptions sendOptions, CancellationToken cancellationToken = default)
        {
            SetPendingTransportOperationsIfNecessary(sendOptions);
            await decoratedMessageSession.Send(message, sendOptions, cancellationToken).ConfigureAwait(false);
        }

        public async Task Send<T>(Action<T> messageConstructor, SendOptions sendOptions, CancellationToken cancellationToken = default)
        {
            SetPendingTransportOperationsIfNecessary(sendOptions);
            await decoratedMessageSession.Send(messageConstructor, sendOptions, cancellationToken).ConfigureAwait(false);
        }

        public async Task Publish(object message, PublishOptions publishOptions, CancellationToken cancellationToken = default)
        {
            SetPendingTransportOperationsIfNecessary(publishOptions);
            await decoratedMessageSession.Publish(message, publishOptions, cancellationToken).ConfigureAwait(false);
        }

        public async Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions,
            CancellationToken cancellationToken = default)
        {
            SetPendingTransportOperationsIfNecessary(publishOptions);
            await decoratedMessageSession.Publish(messageConstructor, publishOptions, cancellationToken).ConfigureAwait(false);
        }

        void SetPendingTransportOperationsIfNecessary(ExtendableOptions sendOptions)
        {
            ContextBag extensions = sendOptions.GetExtensions();
            if (!extensions.TryGet<PendingTransportOperations>(out _))
            {
                extensions.Set(pendingTransportOperations);
            }
        }

        // probably better to not have those in the final version?
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Task Subscribe(Type eventType, SubscribeOptions subscribeOptions, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        // probably better to not have those in the final version?
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Task Unsubscribe(Type eventType, UnsubscribeOptions unsubscribeOptions, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public async Task<IUnitOfWorkMessageSession> Initialize(CancellationToken cancellationToken = default)
        {
            var deduplicationEntry = await outboxStorage.Get(SessionId, contextBag, cancellationToken).ConfigureAwait(false);
            if (deduplicationEntry == null)
            {
                outboxTransaction = await outboxStorage.BeginTransaction(contextBag, cancellationToken)
                    .ConfigureAwait(false);
                contextBag.Set(outboxStorage);
                synchronizedStorageSession = await AdaptOrOpenNewSynchronizedStorageSession(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                ShouldOnlyBeCommitted = true;
            }
            return this;
        }

        public async Task Commit(CancellationToken cancellationToken = default)
        {
            if (!ShouldOnlyBeCommitted)
            {
                var outboxMessage = new OutboxMessage(SessionId, ConvertToOutboxOperations(pendingTransportOperations.Operations));
                await outboxStorage.Store(outboxMessage, outboxTransaction, contextBag, cancellationToken).ConfigureAwait(false);

                await synchronizedStorageSession!.CompleteAsync(cancellationToken).ConfigureAwait(false);
                synchronizedStorageSession!.Dispose();
                synchronizedStorageSession = null;
                await outboxTransaction!.Commit(cancellationToken).ConfigureAwait(false);
                outboxTransaction!.Dispose();
                outboxTransaction = null;
            }

            if (pendingTransportOperations.HasOperations)
            {
                // we just send it again. The inbox will do the rest
                var operation = new Transport.TransportOperation(
                    new OutgoingMessage(SessionId, new Dictionary<string, string>(), ReadOnlyMemory<byte>.Empty),
                    new UnicastAddressTag("self"), requiredDispatchConsistency: DispatchConsistency.Isolated);
                await dispatcher.Dispatch(new TransportOperations(operation), transportTransaction, cancellationToken).ConfigureAwait(false);
            }
        }

        static TransportOperation[] ConvertToOutboxOperations(Transport.TransportOperation[] operations)
        {
            var transportOperations = new TransportOperation[operations.Length];
            var index = 0;
            foreach (var operation in operations)
            {
                SerializeRoutingStrategy(operation.AddressTag, operation.Properties);

                transportOperations[index] = new TransportOperation(operation.Message.MessageId, operation.Properties, operation.Message.Body, operation.Message.Headers);
                index++;
            }
            return transportOperations;
        }

        static void SerializeRoutingStrategy(AddressTag addressTag, Dictionary<string, string> options)
        {
            if (addressTag is MulticastAddressTag indirect)
            {
                options["EventType"] = indirect.MessageType.AssemblyQualifiedName!;
                return;
            }

            if (addressTag is UnicastAddressTag direct)
            {
                options["Destination"] = direct.Destination;
                return;
            }

            throw new Exception($"Unknown routing strategy {addressTag.GetType().FullName}");
        }

        public void Dispose()
        {
            synchronizedStorageSession?.Dispose();
            outboxTransaction?.Dispose();
        }
    }
}