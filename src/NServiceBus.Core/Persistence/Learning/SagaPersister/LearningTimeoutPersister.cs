#nullable enable

namespace NServiceBus;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using Persistence;
using Sagas;

// Timeouts are stored in the saga file, so changes are written by the save or update of the saga in the same session
sealed class LearningTimeoutPersister : ITimeoutPersister
{
    public Task<IReadOnlyList<StoredSagaTimeout>> Get(IContainSagaData sagaData, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<StoredSagaTimeout>>([.. Timeouts(sagaData, session)]);

    public Task Request(IContainSagaData sagaData, StoredSagaTimeout timeout, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
    {
        Timeouts(sagaData, session).Add(timeout);
        return Task.CompletedTask;
    }

    public Task Reschedule(IContainSagaData sagaData, StoredSagaTimeout timeout, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
    {
        var timeouts = Timeouts(sagaData, session);
        var index = timeouts.FindIndex(t => t.TimeoutId == timeout.TimeoutId);
        if (index >= 0)
        {
            timeouts[index] = timeout;
        }

        return Task.CompletedTask;
    }

    public Task Cancel(IContainSagaData sagaData, string timeoutId, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
    {
        _ = Timeouts(sagaData, session).RemoveAll(t => t.TimeoutId == timeoutId);
        return Task.CompletedTask;
    }

    // The saga file keeps only pending timeouts, so a fired timeout is removed like a canceled one
    public Task MarkFired(IContainSagaData sagaData, string timeoutId, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default) =>
        Cancel(sagaData, timeoutId, session, context, cancellationToken);

    static List<StoredSagaTimeout> Timeouts(IContainSagaData sagaData, ISynchronizedStorageSession session) =>
        ((LearningSynchronizedStorageSession)session).GetTimeouts(sagaData);
}
