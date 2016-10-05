namespace NServiceBus.Container
{
    using Settings;

    /// <summary>
    /// Container customization.
    /// </summary>
    public class ContainerCustomizations
    {
        internal ContainerCustomizations(SettingsHolder settings)
        {
            Settings = settings;
        }

        /// <summary>
        /// The settings instance to use to store an existing container instance.
        /// </summary>
        public SettingsHolder Settings { get; private set; }
    }
}