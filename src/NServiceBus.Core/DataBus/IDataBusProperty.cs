namespace NServiceBus;

using System;

/// <summary>
/// The contract to implement a <see cref="IDataBusProperty" />.
/// </summary>
[ObsoleteEx(
    Message = "The DataBus feature is released as a dedicated 'NServiceBus.ClaimCheck.DataBus' package.",
    RemoveInVersion = "11",
    TreatAsErrorFromVersion = "10")]
public interface IDataBusProperty
{
    /// <summary>
    /// The <see cref="IDataBusProperty" /> key.
    /// </summary>
    [ObsoleteEx(
        Message = "The DataBus feature is released as a dedicated 'NServiceBus.ClaimCheck.DataBus' package.",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    string Key { get; set; }

    /// <summary>
    /// <code>true</code> if <see cref="IDataBusProperty" /> has a value.
    /// </summary>
    [ObsoleteEx(
        Message = "The DataBus feature is released as a dedicated 'NServiceBus.ClaimCheck.DataBus' package.",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    bool HasValue { get; set; }

    /// <summary>
    /// Gets the value of the <see cref="IDataBusProperty" />.
    /// </summary>
    [ObsoleteEx(
        Message = "The DataBus feature is released as a dedicated 'NServiceBus.ClaimCheck.DataBus' package.",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    object GetValue();

    /// <summary>
    /// Sets the value for <see cref="IDataBusProperty" />.
    /// </summary>
    [ObsoleteEx(
        Message = "The DataBus feature is released as a dedicated 'NServiceBus.ClaimCheck.DataBus' package.",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    void SetValue(object value);

    /// <summary>
    /// The property <see cref="Type" />.
    /// </summary>
    [ObsoleteEx(
        Message = "The DataBus feature is released as a dedicated 'NServiceBus.ClaimCheck.DataBus' package.",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    Type Type { get; }
}