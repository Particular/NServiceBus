namespace NServiceBus.Sagas;

using System;

/// <summary>
/// The property that this saga is correlated on.
/// </summary>
public class SagaCorrelationProperty
{
    /// <summary>
    /// Initializes the correlation property.
    /// </summary>
    public SagaCorrelationProperty(string name, object value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(value);

        Name = name;
        Value = value;
    }

    /// <summary>
    /// The name of the property.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The property value.
    /// </summary>
    public object Value { get; }

    /// <summary>
    /// Represents a saga with no correlated property.
    /// </summary>
    public static SagaCorrelationProperty None => null;
}