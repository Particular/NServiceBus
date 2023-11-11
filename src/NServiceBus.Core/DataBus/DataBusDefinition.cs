namespace NServiceBus.DataBus;

using System;

/// <summary>
/// Defines a databus that can be used by NServiceBus.
/// </summary>
public abstract class DataBusDefinition
{
    /// <summary>
    /// The feature to enable when this databus is selected.
    /// </summary>
    protected internal abstract Type ProvidedByFeature();
}