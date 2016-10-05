namespace NServiceBus.Configuration.AdvanceExtensibility
{
    using Settings;

    /// <summary>
    /// Base class that exposes <see cref="SettingsHolder" /> for extensibility.
    /// </summary>
    public abstract class ExposeSettings
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ExposeSettings" />.
        /// </summary>
        protected ExposeSettings(SettingsHolder settings)
        {
            Guard.AgainstNull(nameof(settings), settings);
            Settings = settings;
        }

        /// <summary>
        /// Get the current <see cref="SettingsHolder" /> this <see cref="ExposeSettings" /> wraps.
        /// </summary>
        internal SettingsHolder Settings { get; private set; }
    }
}