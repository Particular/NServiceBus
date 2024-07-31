namespace NServiceBus.DataBus;

using System;

/// <summary>
/// Defines a databus that can be used by NServiceBus.
/// </summary>
[ObsoleteEx(
    Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck.DataBus'",
    RemoveInVersion = "11",
    TreatAsErrorFromVersion = "10")]
public abstract class DataBusDefinition
{
    /// <summary>
    /// The feature to enable when this databus is selected.
    /// </summary>
    [ObsoleteEx(
        Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck.DataBus'",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    protected internal abstract Type ProvidedByFeature();
}