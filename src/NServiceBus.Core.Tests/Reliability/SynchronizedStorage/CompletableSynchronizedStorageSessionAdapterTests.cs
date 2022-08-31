namespace NServiceBus.Core.Tests.Reliability
{
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Outbox;
    using NServiceBus.Persistence;
    using NUnit.Framework;
    using Transport;

    [TestFixture]
    public class CompletableSynchronizedStorageSessionAdapterTests
    {
        [Test]
        public async Task Should_dispose_adapted_session_only_once()
        {
            var storageAdapter = new FakeStorageAdapter();
            var sessionAdapter = new CompletableSynchronizedStorageSessionAdapter(storageAdapter, null);
            await sessionAdapter.TryOpen(new TransportTransaction(), new ContextBag());

            sessionAdapter.Dispose();
            sessionAdapter.Dispose();

            Assert.AreEqual(1, storageAdapter.StorageSession.DisposeCounter);
        }
    }

    public class FakeStorageAdapter : ISynchronizedStorageAdapter
    {
        public FakeStorageSession StorageSession { get; } = new FakeStorageSession();

        public Task<CompletableSynchronizedStorageSession> TryAdapt(OutboxTransaction transaction, ContextBag context) => Task.FromResult<CompletableSynchronizedStorageSession>(StorageSession);

        public Task<CompletableSynchronizedStorageSession> TryAdapt(TransportTransaction transportTransaction, ContextBag context) => Task.FromResult<CompletableSynchronizedStorageSession>(StorageSession);
    }

    public class FakeStorageSession : CompletableSynchronizedStorageSession
    {
        public int DisposeCounter { get; private set; }

        public void Dispose() => DisposeCounter++;

        public Task CompleteAsync() => Task.FromResult(0);
    }
}