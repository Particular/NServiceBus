#nullable enable

namespace NServiceBus;

using System;
using Particular.Obsoletes;
using Settings;

/// <summary>
/// Indicates that this class contains logic that needs to run just before
/// configuration is finalized.
/// </summary>
[ObsoleteMetadata(Message = "Final adjustments to settings before configuration is finalized should be applied via an explicit last configuration step on the endpoint configuration, instead of via implementations of this interface discovered by scanning", TreatAsErrorFromVersion = "11.0", RemoveInVersion = "12.0")]
[Obsolete("Final adjustments to settings before configuration is finalized should be applied via an explicit last configuration step on the endpoint configuration, instead of via implementations of this interface discovered by scanning. Will be treated as an error from version 11.0.0. Will be removed in version 12.0.0.", false)]
public interface IWantToRunBeforeConfigurationIsFinalized
{
    /// <summary>
    /// Invoked before configuration is finalized and locked.
    /// </summary>
    void Run(SettingsHolder settings);
}