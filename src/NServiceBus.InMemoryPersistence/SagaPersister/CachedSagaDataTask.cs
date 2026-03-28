namespace NServiceBus.Persistence.InMemory;

using System.Threading.Tasks;

static class CachedSagaDataTask<TSagaData>
    where TSagaData : IContainSagaData
{
    public static readonly Task<TSagaData?> Default = Task.FromResult(default(TSagaData));
}