#nullable enable

namespace NServiceBus.Logging;

using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Provides endpoint context information that can be resolved from DI for scope enrichment of logs.
/// </summary>
public sealed class EndpointLoggingScope : IReadOnlyList<KeyValuePair<string, object?>>
{
    /// <summary>
    /// The name of the endpoint.
    /// </summary>
    public required string EndpointName { get; init; }

    /// <summary>
    /// An optional identifier for the endpoint, useful for distinguishing between
    /// multiple instances of the same endpoint.
    /// </summary>
    public object? EndpointIdentifier { get; init; }

    /// <summary>
    /// Returns a string representation of the endpoint logging scope.
    /// </summary>
    public override string ToString() => EndpointIdentifier is null
        ? $"Endpoint = {EndpointName}"
        : $"Endpoint = {EndpointName}, EndpointIdentifier = {EndpointIdentifier}";

    KeyValuePair<string, object?> IReadOnlyList<KeyValuePair<string, object?>>.this[int index] => Entries[index];

    int IReadOnlyCollection<KeyValuePair<string, object?>>.Count => Entries.Length;

    IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator() =>
        ((IEnumerable<KeyValuePair<string, object?>>)Entries).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => Entries.GetEnumerator();

    KeyValuePair<string, object?>[] Entries => field ??= LogScopeStates.BuildScopeEntries(EndpointName, EndpointIdentifier);
}
