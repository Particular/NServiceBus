#nullable enable

namespace NServiceBus;

using System;
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
    public override KeyValuePair<string, object?> this[int index] =>
        index switch
        {
            0 => new KeyValuePair<string, object?>("Endpoint", endpointName),
            1 when endpointIdentifier is not null => new KeyValuePair<string, object?>("EndpointIdentifier", endpointIdentifier),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public override int Count => endpointIdentifier is null ? 1 : 2;

    public override IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
    {
        yield return this[0];

        if (endpointIdentifier is not null)
        {
            yield return this[1];
        }
    }

    public override string ToString() => endpointIdentifier is null
        ? $"Endpoint = {endpointName}"
        : $"Endpoint = {endpointName}, EndpointIdentifier = {endpointIdentifier}";
}

sealed class ExtendedLogScopeState(LogScopeState parentScope, string key, object value) : LogScopeState
{
    public override KeyValuePair<string, object?> this[int index] =>
        index == parentScope.Count
            ? extra
            : parentScope[index];

    public override int Count => parentScope.Count + 1;

    public override IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
    {
        for (var i = 0; i < parentScope.Count; i++)
        {
            yield return parentScope[i];
        }

        yield return extra;
    }

    public override string ToString() => $"{parentScope}, {key} = {value}";

    readonly KeyValuePair<string, object?> extra = new(key, value);
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