namespace NServiceBus.Core.Tests.Fakes
{
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Persistence;

    public class FakeSynchronizedStorage : ISynchronizedStorage
    {
        public Task<CompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag)
        {
            var session = (CompletableSynchronizedStorageSession)new FakeSynchronizedStorageSession();
            return Task.FromResult(session);
        }
    }
}
