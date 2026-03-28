namespace NServiceBus.PersistenceTesting;

using System;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Outbox;
using NServiceBus.Persistence;
using NServiceBus.Persistence.InMemory;
using NServiceBus.Sagas;

public partial class PersistenceTestsConfiguration
{
    public bool SupportsDtc => false;

    public bool SupportsOutbox => true;

    public bool SupportsFinders => false;

    public bool SupportsPessimisticConcurrency => false;

    public ISagaIdGenerator SagaIdGenerator { get; private set; }

    public ISagaPersister SagaStorage { get; private set; }

    public IOutboxStorage OutboxStorage { get; private set; }

    public Func<ICompletableSynchronizedStorageSession> CreateStorageSession { get; private set; }

    public Task Configure(CancellationToken cancellationToken = default)
    {
        SagaIdGenerator = new TestSagaIdGenerator();

        var settings = new InMemorySagaPersisterSettings(
            new System.Text.Json.JsonSerializerOptions
            {
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            });

        SagaStorage = new InMemorySagaPersister(settings);
        OutboxStorage = new InMemoryOutboxStorage();
        CreateStorageSession = () => new InMemorySynchronizedStorageSession();

        return Task.CompletedTask;
    }

    public Task Cleanup(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

class TestSagaIdGenerator : ISagaIdGenerator
{
    public Guid Generate(SagaIdGeneratorContext context) => Guid.NewGuid();
}
