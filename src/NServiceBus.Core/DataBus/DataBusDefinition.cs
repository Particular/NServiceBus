namespace NServiceBus.DataBus;

using System;

/// <summary>
/// Defines a databus that can be used by NServiceBus.
/// </summary>
[ObsoleteEx(
    Message = "The DataBus feature is released as a dedicated 'NServiceBus.ClaimCheck.DataBus' package.",
    RemoveInVersion = "11",
    TreatAsErrorFromVersion = "10")]
public abstract class DataBusDefinition
{
    /// <summary>
    /// The feature to enable when this databus is selected.
    /// </summary>
    [ObsoleteEx(
    Message = "The DataBus feature is released as a dedicated 'NServiceBus.ClaimCheck.DataBus' package.",
    RemoveInVersion = "11",
    TreatAsErrorFromVersion = "10")]
    protected internal abstract Type ProvidedByFeature();
}