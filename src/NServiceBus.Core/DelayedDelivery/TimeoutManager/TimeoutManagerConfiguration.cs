namespace NServiceBus
{
    using Settings;

    /// <summary>
    /// Allows configuration of the timeout manager.
    /// </summary>
    public class TimeoutManagerConfiguration
    {
        internal SettingsHolder settings;

        /// <summary>
        /// Creates a new instance of <see cref="TimeoutManagerConfiguration"/>.
        /// </summary>
        /// <param name="settings"></param>
        public TimeoutManagerConfiguration(SettingsHolder settings)
        {
            this.settings = settings;
        }
    }
}