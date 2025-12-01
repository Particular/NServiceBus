#nullable enable

namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using Persistence;
using Sagas;

sealed class LearningSagaPersister(SagaManifestCollection sagaManifests) : ISagaPersister
{
    public Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
    {
        var storageSession = (LearningSynchronizedStorageSession)session;
        storageSession.Save(sagaData, sagaManifests);
        return Task.CompletedTask;
    }

    public Task Update(IContainSagaData sagaData, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
    {
        var storageSession = (LearningSynchronizedStorageSession)session;
        storageSession.Update(sagaData, sagaManifests);
        return Task.CompletedTask;
    }

    public Task<TSagaData> Get<TSagaData>(Guid sagaId, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
        where TSagaData : class, IContainSagaData =>
        Get<TSagaData>(sagaId, session, sagaManifests, cancellationToken);

    public Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
        where TSagaData : class, IContainSagaData =>
        Get<TSagaData>(LearningSagaIdGenerator.Generate(typeof(TSagaData), propertyName, propertyValue), session, sagaManifests, cancellationToken);

    public Task Complete(IContainSagaData sagaData, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
    {
        var storageSession = (LearningSynchronizedStorageSession)session;
        storageSession.Complete(sagaData, sagaManifests);
        return Task.CompletedTask;
    }

    static Task<TSagaData> Get<TSagaData>(Guid sagaId, ISynchronizedStorageSession session, SagaManifestCollection sagaManifests, CancellationToken cancellationToken) where TSagaData : class, IContainSagaData
    {
        var storageSession = (LearningSynchronizedStorageSession)session;
        return storageSession.Read<TSagaData>(sagaId, sagaManifests, cancellationToken)!;
    }
}