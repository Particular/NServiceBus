namespace NServiceBus;

using System;
using DataBus;
using Features;

/// <summary>
/// Base class for data bus definitions.
/// </summary>
[ObsoleteEx(
    Message = "The DataBus feature is released as a dedicated 'NServiceBus.ClaimCheck.DataBus' package.",
    RemoveInVersion = "11",
    TreatAsErrorFromVersion = "10")]
public class FileShareDataBus : DataBusDefinition
{
    /// <summary>
    /// The feature to enable when this databus is selected.
    /// </summary>
    [ObsoleteEx(
        Message = "The DataBus feature is released as a dedicated 'NServiceBus.ClaimCheck.DataBus' package.",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    protected internal override Type ProvidedByFeature()
    {
        return typeof(DataBusFileBased);
    }
}