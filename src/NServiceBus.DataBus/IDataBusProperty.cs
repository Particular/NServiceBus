namespace NServiceBus;

using System;

/// <summary>
/// The contract to implement a <see cref="IDataBusProperty" />.
/// </summary>
public interface IDataBusProperty
{
    /// <summary>
    /// The <see cref="IDataBusProperty" /> key.
    /// </summary>
    string Key { get; set; }

    /// <summary>
    /// <code>true</code> if <see cref="IDataBusProperty" /> has a value.
    /// </summary>
    bool HasValue { get; set; }

    /// <summary>
    /// Gets the value of the <see cref="IDataBusProperty" />.
    /// </summary>
    object GetValue();

    /// <summary>
    /// Sets the value for <see cref="IDataBusProperty" />.
    /// </summary>
    void SetValue(object value);

    /// <summary>
    /// The property <see cref="Type" />.
    /// </summary>
    Type Type { get; }
}