namespace NServiceBus.Core.Tests.Fakes
{
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Persistence;

    public class FakeSynchronizedStorage : ISynchronizedStorage
    {
        public Task<ICompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag, CancellationToken cancellationToken = default)
        {
            var session = (ICompletableSynchronizedStorageSession)new FakeSynchronizedStorageSession();
            return Task.FromResult(session);
        }
    }
}