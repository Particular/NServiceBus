#nullable enable

namespace NServiceBus;

/// <summary>
/// Accessor for the correlation property value.
/// </summary>
public abstract class CorrelationPropertyAccessor
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="sagaData"></param>
    /// <param name="value"></param>
    public abstract void WriteTo(IContainSagaData sagaData, object value);

    /// <summary>
    ///
    /// </summary>
    /// <param name="sagaData"></param>
    /// <returns></returns>
    public abstract object? AccessFrom(IContainSagaData sagaData);
}