#nullable enable

namespace NServiceBus.Logging;

using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Carries the endpoint identity for log scope enrichment.
/// Resolve from dependency injection and pass to
/// <see cref="EndpointLoggingScopeExtensions.BeginEndpointScope" /> to attach the endpoint
/// name and identifier to log output.
/// </summary>
public sealed class EndpointLoggingScope : IReadOnlyList<KeyValuePair<string, object?>>
{
    /// <summary>
    /// The name of the endpoint.
    /// </summary>
    public required string EndpointName { get; init; }

    /// <summary>
    /// A unique identifier for the endpoint instance, useful for telling apart
    /// multiple instances of the same endpoint name.
    /// </summary>
    public object? EndpointIdentifier { get; init; }

    /// <summary>
    /// The logging slot for this endpoint, carrying both the slot key (for routing) and
    /// scope state (for enrichment). Set by the hosting infrastructure during DI registration.
    /// </summary>
    internal LogSlot? Slot { get; init; }

    /// <summary>
    /// The scope state for this endpoint, delegating to the slot's state when available,
    /// otherwise computing from <see cref="EndpointName" /> and <see cref="EndpointIdentifier" />.
    /// </summary>
    LogScopeState ScopeState => Slot?.ScopeState ?? new LogScopeStates(EndpointName, EndpointIdentifier);

    /// <summary>
    /// Returns a string representation of the endpoint logging scope.
    /// </summary>
    public override string ToString() => ScopeState.ToString()!;

    KeyValuePair<string, object?> IReadOnlyList<KeyValuePair<string, object?>>.this[int index] => ScopeState[index];

    int IReadOnlyCollection<KeyValuePair<string, object?>>.Count => ScopeState.Count;

    IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator() => ScopeState.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ScopeState.GetEnumerator();
}