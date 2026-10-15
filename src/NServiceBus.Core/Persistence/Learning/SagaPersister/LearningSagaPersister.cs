#nullable enable

namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using Persistence;
using Sagas;

sealed class LearningSagaPersister(SagaManifestCollection sagaManifests) : ISagaPersister, ISupportTimeoutManagement
{
    public Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
    {
        var storageSession = (LearningSynchronizedStorageSession)session;
        storageSession.Save(sagaData, context.GetSagaTimeouts(sagaData), sagaManifests);
        return Task.CompletedTask;
    }

    public Task Update(IContainSagaData sagaData, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
    {
        var storageSession = (LearningSynchronizedStorageSession)session;
        storageSession.Update(sagaData, context.GetSagaTimeouts(sagaData), sagaManifests);
        return Task.CompletedTask;
    }

    public Task<TSagaData> Get<TSagaData>(Guid sagaId, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
        where TSagaData : class, IContainSagaData =>
        Get<TSagaData>(sagaId, session, context, sagaManifests, cancellationToken);

    public Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
        where TSagaData : class, IContainSagaData =>
        Get<TSagaData>(LearningSagaIdGenerator.Generate(typeof(TSagaData), propertyName, propertyValue), session, context, sagaManifests, cancellationToken);

    public Task Complete(IContainSagaData sagaData, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
    {
        var storageSession = (LearningSynchronizedStorageSession)session;
        storageSession.Complete(sagaData, sagaManifests);
        return Task.CompletedTask;
    }

    static async Task<TSagaData> Get<TSagaData>(Guid sagaId, ISynchronizedStorageSession session, ContextBag context, SagaManifestCollection sagaManifests, CancellationToken cancellationToken) where TSagaData : class, IContainSagaData
    {
        var storageSession = (LearningSynchronizedStorageSession)session;
        var (sagaData, timeouts) = await storageSession.Read<TSagaData>(sagaId, sagaManifests, cancellationToken).ConfigureAwait(false);

        if (sagaData is not null)
        {
            context.SetSagaTimeouts(sagaData, timeouts);
        }

        return sagaData!;
    }
}