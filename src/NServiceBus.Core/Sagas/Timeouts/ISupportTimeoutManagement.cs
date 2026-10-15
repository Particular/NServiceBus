#nullable enable

namespace NServiceBus.Sagas;

using System.Collections.Generic;
using Extensibility;

/// <summary>
/// Implemented by saga persisters that store saga timeouts atomically with the saga data.
/// </summary>
/// <remarks>
/// When loading a saga, the persister passes the stored timeouts to Core with <see cref="SagaTimeoutStorageExtensions.SetSagaTimeouts" />.
/// When saving or updating a saga, the persister reads them with <see cref="SagaTimeoutStorageExtensions.GetSagaTimeouts" />.
/// </remarks>
public interface ISupportTimeoutManagement;

/// <summary>
/// Passes saga timeouts between Core and persisters that implement <see cref="ISupportTimeoutManagement" />.
/// </summary>
public static class SagaTimeoutStorageExtensions
{
    /// <summary>
    /// Gets the timeouts to store with the saga data.
    /// </summary>
    public static IReadOnlyList<StoredSagaTimeout> GetSagaTimeouts(this ContextBag context, IContainSagaData sagaData) =>
        context.TryGet<List<StoredSagaTimeout>>(Key(sagaData), out var timeouts) ? timeouts : [];

    /// <summary>
    /// Sets the timeouts loaded with the saga data.
    /// </summary>
    public static void SetSagaTimeouts(this ContextBag context, IContainSagaData sagaData, IEnumerable<StoredSagaTimeout> timeouts) =>
        context.Set(Key(sagaData), new List<StoredSagaTimeout>(timeouts));

    internal static List<StoredSagaTimeout> GetOrCreateSagaTimeouts(this ContextBag context, IContainSagaData sagaData)
    {
        if (!context.TryGet<List<StoredSagaTimeout>>(Key(sagaData), out var timeouts))
        {
            timeouts = [];
            context.Set(Key(sagaData), timeouts);
        }

        return timeouts;
    }

    static string Key(IContainSagaData sagaData) => $"NServiceBus.SagaTimeouts:{sagaData.GetType().FullName}:{sagaData.Id}";
}
