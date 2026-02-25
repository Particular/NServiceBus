#nullable enable

namespace NServiceBus.Logging;

using System;
using System.Collections;
using System.Collections.Generic;

sealed class EndpointLogScopeState(object endpointName, object? endpointIdentifier) : IReadOnlyList<KeyValuePair<string, object?>>
{
    public KeyValuePair<string, object?> this[int index] =>
        index switch
        {
            0 => new KeyValuePair<string, object?>("Endpoint", endpointName),
            1 when endpointIdentifier is not null => new KeyValuePair<string, object?>("EndpointIdentifier", endpointIdentifier),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public int Count => endpointIdentifier is null ? 1 : 2;

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
    {
        yield return this[0];

        if (endpointIdentifier is not null)
        {
            yield return this[1];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override string ToString() => endpointIdentifier is null
        ? $"Endpoint = {endpointName}"
        : $"Endpoint = {endpointName}, EndpointIdentifier = {endpointIdentifier}";
}

sealed class EndpointLogSlot(string endpointName, object? endpointIdentifier)
{
    public EndpointLogScopeState ScopeState { get; } = new(endpointName, endpointIdentifier);
}