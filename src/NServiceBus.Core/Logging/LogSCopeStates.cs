#nullable enable

namespace NServiceBus;

using System.Collections;
using System.Collections.Generic;

abstract class LogScopeState : IReadOnlyList<KeyValuePair<string, object?>>
{
    public abstract KeyValuePair<string, object?> this[int index] { get; }

    public abstract int Count { get; }

    public abstract IEnumerator<KeyValuePair<string, object?>> GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

abstract class LogSlot
{
    public abstract LogScopeState ScopeState { get; }
}

sealed class LogSCopeStates(object endpointName, object? endpointIdentifier) : LogScopeState
{
    public override KeyValuePair<string, object?> this[int index] => entries[index];

    public override int Count => entries.Length;

    public override IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => ((IEnumerable<KeyValuePair<string, object?>>)entries).GetEnumerator();

    public override string ToString() => endpointIdentifier is null
        ? $"Endpoint = {endpointName}"
        : $"Endpoint = {endpointName}, EndpointIdentifier = {endpointIdentifier}";

    readonly KeyValuePair<string, object?>[] entries = endpointIdentifier is null
        ? [new KeyValuePair<string, object?>("Endpoint", endpointName)]
        :
        [
            new KeyValuePair<string, object?>("Endpoint", endpointName),
            new KeyValuePair<string, object?>("EndpointIdentifier", endpointIdentifier)
        ];
}

sealed class ExtendedLogScopeState(LogScopeState parentScope, string key, object value) : LogScopeState
{
    public override KeyValuePair<string, object?> this[int index] => entries[index];

    public override int Count => entries.Length;

    public override IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => ((IEnumerable<KeyValuePair<string, object?>>)entries).GetEnumerator();

    public override string ToString() => $"{parentScope}, {key} = {value}";

    readonly KeyValuePair<string, object?>[] entries = BuildEntries(parentScope, key, value);

    static KeyValuePair<string, object?>[] BuildEntries(LogScopeState parentScope, string key, object value)
    {
        var parentCount = parentScope.Count;
        var scopeEntries = new KeyValuePair<string, object?>[parentCount + 1];

        for (var i = 0; i < parentCount; i++)
        {
            scopeEntries[i] = parentScope[i];
        }

        scopeEntries[parentCount] = new KeyValuePair<string, object?>(key, value);
        return scopeEntries;
    }
}

sealed class EndpointLogSlot(string endpointName, object? endpointIdentifier) : LogSlot
{
    public override LogScopeState ScopeState { get; } = new LogSCopeStates(endpointName, endpointIdentifier);
}

sealed class EndpointSatelliteLogSlot(EndpointLogSlot endpointSlot, string satelliteName) : LogSlot
{
    public override LogScopeState ScopeState { get; } = new ExtendedLogScopeState(endpointSlot.ScopeState, "Satellite", satelliteName);
}

sealed class EndpointReceiverLogSlot(EndpointLogSlot endpointSlot, string receiverName) : LogSlot
{
    public override LogScopeState ScopeState { get; } = new ExtendedLogScopeState(endpointSlot.ScopeState, "Receiver", receiverName);
}