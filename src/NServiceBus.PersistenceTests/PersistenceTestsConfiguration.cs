namespace NServiceBus.PersistenceTesting;

using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Outbox;
using NServiceBus.Sagas;
using NUnit.Framework;
using Persistence;
using Utils;

public partial class PersistenceTestsConfiguration
{
    public bool SupportsDtc => false;

    public bool SupportsOutbox => false;

    public bool SupportsFinders => false;

    public bool SupportsPessimisticConcurrency => true;

    public ISagaIdGenerator SagaIdGenerator { get; private set; }

    public ISagaPersister SagaStorage { get; private set; }

    public IOutboxStorage OutboxStorage { get; private set; }

    public Func<ICompletableSynchronizedStorageSession> CreateStorageSession { get; private set; }

    public Task Configure(CancellationToken cancellationToken = default)
    {
        SagaIdGenerator = new LearningSagaIdGenerator();

        storageLocation = Path.Combine(Path.GetTempPath(), ".sagas", TestContext.CurrentContext.Test.ID);

        var sagaManifests = new SagaManifestCollection(SagaMetadataCollection,
            storageLocation,
            name => DeterministicGuid.Create(name).ToString(),
            JsonSerializerOptions.Default);

        SagaStorage = new LearningSagaPersister(sagaManifests);

        CreateStorageSession = () => new LearningSynchronizedStorageSession();

        return Task.CompletedTask;
    }

    public Task Cleanup(CancellationToken cancellationToken = default)
    {
        if (storageLocation != null && Directory.Exists(storageLocation))
        {
            Directory.Delete(storageLocation, true);
        }

        return Task.CompletedTask;
    }

    string storageLocation;
}