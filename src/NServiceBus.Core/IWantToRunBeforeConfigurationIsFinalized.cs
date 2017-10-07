namespace NServiceBus
{
    using Settings;

    /// <summary>
    /// Indicates that this class contains logic that needs to run just before
    /// configuration is finalized.
    /// </summary>
    public interface IWantToRunBeforeConfigurationIsFinalized
    {
        /// <summary>
        /// Invoked before configuration is finalized and locked.
        /// </summary>
        void Run(SettingsHolder settings);
    }
}