#nullable enable

namespace NServiceBus;

/// <summary>
/// Accessor for the correlation property value.
/// </summary>
public abstract class CorrelationPropertyAccessor
{
    /// <summary>
    /// Writes the value to the saga data's correlation property.
    /// </summary>
    /// <param name="sagaData">The saga data to write correlation property value to.</param>
    /// <param name="value">The correlation property value to be written.</param>
    public abstract void WriteTo(IContainSagaData sagaData, object value);

    /// <summary>
    /// Accesses the value from the saga data.
    /// </summary>
    /// <param name="sagaData">The saga data to access the correlation property value from.</param>
    /// <returns>The value of the saga data correlation property or null.</returns>
    public abstract object? AccessFrom(IContainSagaData sagaData);
}