namespace NServiceBus;

using System;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

/// <summary>
/// Default implementation for <see cref="IDataBusProperty" />.
/// </summary>
/// <typeparam name="T">Type of data to store in <see cref="IDataBusProperty" />.</typeparam>
[ObsoleteEx(
    Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck.DataBus'",
    RemoveInVersion = "11",
    TreatAsErrorFromVersion = "10")]
public class DataBusProperty<T> : IDataBusProperty where T : class
{
    /// <summary>
    /// Initializes a <see cref="DataBusProperty{T}" /> with no value set.
    /// </summary>
    [ObsoleteEx(
        Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck.DataBus'",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    public DataBusProperty() { }

    /// <summary>
    /// Initializes a <see cref="DataBusProperty{T}" /> with the <paramref name="value" />.
    /// </summary>
    /// <param name="value">The value to initialize with.</param>
    [ObsoleteEx(
        Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck.DataBus'",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    public DataBusProperty(T value) => SetValue(value);

    /// <summary>
    /// The value.
    /// </summary>
    [JsonIgnore]
    [XmlIgnore]
    [ObsoleteEx(
        Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck.DataBus'",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    public T Value { get; private set; }

    /// <summary>
    /// The property <see cref="Type" />.
    /// </summary>
    [JsonIgnore]
    [ObsoleteEx(
        Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck.DataBus'",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    public Type Type { get; } = typeof(T);

    /// <summary>
    /// The <see cref="IDataBusProperty" /> key.
    /// </summary>
    [ObsoleteEx(
        Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck.DataBus'",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    public string Key { get; set; }

    /// <summary>
    /// <code>true</code> if <see cref="IDataBusProperty" /> has a value.
    /// </summary>
    [ObsoleteEx(
        Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck.DataBus'",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    public bool HasValue { get; set; }

    /// <summary>
    /// Sets the value for <see cref="IDataBusProperty" />.
    /// </summary>
    /// <param name="valueToSet">The value to set.</param>
    [ObsoleteEx(
        Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck.DataBus'",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    public void SetValue(object valueToSet)
    {
        Value = valueToSet as T;
        HasValue = Value != null;
    }

    /// <summary>
    /// Gets the value of the <see cref="IDataBusProperty" />.
    /// </summary>
    /// <returns>The value.</returns>
    [ObsoleteEx(
        Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck.DataBus'",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    public object GetValue() => Value;
}