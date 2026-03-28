namespace NServiceBus.Persistence.InMemory;

using System;
using Sagas;

/// <summary>
/// Value type for correlation property lookup.
/// The type and propertyName are not allocated (they are stored in saga metadata).
/// Only the CorrelationId instance and propertyValue are allocated.
/// </summary>
readonly struct CorrelationId(Type type, string propertyName, object propertyValue) : IEquatable<CorrelationId>
{
    public CorrelationId(Type sagaType, SagaCorrelationProperty correlationProperty)
        : this(sagaType, correlationProperty.Name, correlationProperty.Value)
    {
    }

    public bool Equals(CorrelationId other)
        => type == other.type
        && string.Equals(propertyName, other.propertyName, StringComparison.Ordinal)
        && propertyValue.Equals(other.propertyValue);

    public override bool Equals(object? obj) =>
        obj switch
        {
            null => false,
            CorrelationId other => Equals(other),
            _ => false
        };

    public override int GetHashCode() => HashCode.Combine(type, propertyValue);

    readonly Type type = type;
    readonly string propertyName = propertyName;
    readonly object propertyValue = propertyValue;
}
