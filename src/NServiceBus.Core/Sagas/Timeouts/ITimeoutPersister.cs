#nullable enable

namespace NServiceBus.Sagas;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using Persistence;

/// <summary>
/// Stores the timeouts of saga instances. A persistence whose definition overrides <see cref="PersistenceDefinition.SupportsTimeoutManagement" /> to return true registers an implementation in the service collection.
/// </summary>
/// <remarks>
/// All operations are enlisted in the synchronized storage session so that timeout changes commit or roll back together with the saga changes of the same message.
/// Core calls <see cref="Request" />, <see cref="Reschedule" />, <see cref="Cancel" />, and <see cref="MarkFired" /> after <see cref="ISagaPersister.Save" /> or <see cref="ISagaPersister.Update" />
/// for the same saga, and doesn't call them for a saga that completes. <see cref="ISagaPersister.Complete" /> must remove the timeouts of the completed saga.
/// </remarks>
public interface ITimeoutPersister
{
    /// <summary>
    /// Gets the pending timeouts of a saga that was loaded in the same <paramref name="session" />.
    /// </summary>
    Task<IReadOnlyList<StoredSagaTimeout>> Get(IContainSagaData sagaData, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a new timeout of the saga.
    /// </summary>
    Task Request(IContainSagaData sagaData, StoredSagaTimeout timeout, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces a stored timeout of the saga that has the same <see cref="StoredSagaTimeout.TimeoutId" />.
    /// </summary>
    Task Reschedule(IContainSagaData sagaData, StoredSagaTimeout timeout, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a stored timeout of the saga because it was canceled. Does nothing if no timeout has the given ID.
    /// </summary>
    Task Cancel(IContainSagaData sagaData, string timeoutId, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a stored timeout of the saga because the saga handled its timeout message. Does nothing if no timeout has the given ID.
    /// </summary>
    Task MarkFired(IContainSagaData sagaData, string timeoutId, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default);
}
