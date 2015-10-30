namespace NServiceBus
{
    using JetBrains.Annotations;
    using NServiceBus.Settings;

    /// <summary>
    /// Indicates that this class contains logic that needs to run just before
    /// configuration is finalized.
    /// </summary>
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public interface IFinalizeConfiguration
    {
        /// <summary>
        /// Invoked before configuration is finalized and locked.
        /// </summary>
        void Run(SettingsHolder settings);
    }
}