#nullable enable

namespace NServiceBus.Logging;

using System;
using System.Collections;
using System.Collections.Generic;

sealed class EndpointLogScopeState(object endpointName, object? endpointIdentifier) : IReadOnlyList<KeyValuePair<string, object?>>
{
    public static EndpointLogScopeState ForSatellite(object endpointName, object? endpointIdentifier, string satelliteName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(satelliteName);
        return new EndpointLogScopeState(endpointName, endpointIdentifier, receiverName: null, satelliteName);
    }

    public static EndpointLogScopeState ForReceiver(object endpointName, object? endpointIdentifier, string receiverName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(receiverName);
        return new EndpointLogScopeState(endpointName, endpointIdentifier, receiverName, satelliteName: null);
    }

    public KeyValuePair<string, object?> this[int index] =>
        index switch
        {
            0 => new KeyValuePair<string, object?>("Endpoint", endpointName),
            1 when endpointIdentifier is not null => new KeyValuePair<string, object?>("EndpointIdentifier", endpointIdentifier),
            1 when receiverName is not null => new KeyValuePair<string, object?>("Receiver", receiverName),
            1 when satelliteName is not null => new KeyValuePair<string, object?>("Satellite", satelliteName),
            2 when endpointIdentifier is not null && receiverName is not null => new KeyValuePair<string, object?>("Receiver", receiverName),
            2 when endpointIdentifier is not null && satelliteName is not null => new KeyValuePair<string, object?>("Satellite", satelliteName),
            3 when endpointIdentifier is not null && receiverName is not null && satelliteName is not null => new KeyValuePair<string, object?>("Satellite", satelliteName),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public int Count
    {
        get
        {
            var count = endpointIdentifier is null ? 1 : 2;
            if (satelliteName is not null)
            {
                count++;
            }

            if (receiverName is not null)
            {
                count++;
            }

            return count;
        }
    }

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
    {
        yield return this[0];

        if (endpointIdentifier is not null)
        {
            yield return this[1];
        }

        if (receiverName is not null)
        {
            yield return this[endpointIdentifier is null ? 1 : 2];
        }

        if (satelliteName is not null)
        {
            var index = endpointIdentifier is null
                ? receiverName is null ? 1 : 2
                : receiverName is null ? 2 : 3;
            yield return this[index];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override string ToString() => endpointIdentifier is null
        ? satelliteName is null
            ? $"Endpoint = {endpointName}"
            : receiverName is null
                ? $"Endpoint = {endpointName}, Satellite = {satelliteName}"
                : $"Endpoint = {endpointName}, Receiver = {receiverName}, Satellite = {satelliteName}"
        : satelliteName is null
            ? receiverName is null
                ? $"Endpoint = {endpointName}, EndpointIdentifier = {endpointIdentifier}"
                : $"Endpoint = {endpointName}, EndpointIdentifier = {endpointIdentifier}, Receiver = {receiverName}"
            : receiverName is null
                ? $"Endpoint = {endpointName}, EndpointIdentifier = {endpointIdentifier}, Satellite = {satelliteName}"
                : $"Endpoint = {endpointName}, EndpointIdentifier = {endpointIdentifier}, Receiver = {receiverName}, Satellite = {satelliteName}";

    EndpointLogScopeState(object endpointName, object? endpointIdentifier, string? receiverName, string? satelliteName)
        : this(endpointName, endpointIdentifier)
    {
        this.receiverName = receiverName;
        this.satelliteName = satelliteName;
    }

    readonly string? receiverName;
    readonly string? satelliteName;
}

sealed class EndpointLogSlot(string endpointName, object? endpointIdentifier)
{
    public object EndpointName { get; } = endpointName;
    public object? EndpointIdentifier { get; } = endpointIdentifier;
    public EndpointLogScopeState ScopeState { get; } = new(endpointName, endpointIdentifier);
}

sealed class EndpointSatelliteLogSlot(EndpointLogSlot endpointSlot, string satelliteName)
{
    public EndpointLogScopeState ScopeState { get; } = EndpointLogScopeState.ForSatellite(endpointSlot.EndpointName, endpointSlot.EndpointIdentifier, satelliteName);
}

sealed class EndpointReceiverLogSlot(EndpointLogSlot endpointSlot, string receiverName)
{
    public EndpointLogScopeState ScopeState { get; } = EndpointLogScopeState.ForReceiver(endpointSlot.EndpointName, endpointSlot.EndpointIdentifier, receiverName);
}