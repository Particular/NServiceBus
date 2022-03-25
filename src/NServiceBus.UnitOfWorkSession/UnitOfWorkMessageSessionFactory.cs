namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Outbox;
    using Persistence;
    using Transport;

    class UnitOfWorkMessageSessionFactory : IUnitOfWorkMessageSessionFactory
    {
        IMessageSession? messageSession;
        IMessageDispatcher? dispatcher;
        IOutboxStorage? outboxStorage;
        ISynchronizedStorage? synchronizedStorage;
        ISynchronizedStorageAdapter? synchronizedStorageAdapter;
        string? queueAddress;

        public Task<IUnitOfWorkMessageSession> OpenSession(string? sessionId = default, CancellationToken cancellationToken = default)
        {
            var session = new UnitOfWorkMessageSession(queueAddress!, messageSession!, dispatcher!, outboxStorage, synchronizedStorageAdapter, synchronizedStorage, sessionId ?? Guid.NewGuid().ToString());
            return session.Initialize(cancellationToken);
        }

        public void Initialize(string queueAddress, IMessageSession messageSession,
            IMessageDispatcher dispatcher,
            IOutboxStorage? outboxStorage, ISynchronizedStorageAdapter? synchronizedStorageAdapter,
            ISynchronizedStorage? synchronizedStorage)
        {
            this.queueAddress = queueAddress;
            this.synchronizedStorageAdapter = synchronizedStorageAdapter;
            this.synchronizedStorage = synchronizedStorage;
            this.messageSession = messageSession;
            this.dispatcher = dispatcher;
            this.outboxStorage = outboxStorage;
        }
    }
}