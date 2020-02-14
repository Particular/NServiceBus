namespace NServiceBus.Container
{
    using Settings;

    /// <summary>
    /// Container customization.
    /// </summary>
    [ObsoleteEx(
        Message = "Support for custom dependency injection containers is provided via the NServiceBus.Extensions.DependencyInjection package.",
        RemoveInVersion = "9.0.0",
        TreatAsErrorFromVersion = "8.0.0")]
    public class ContainerCustomizations
    {
        internal ContainerCustomizations(SettingsHolder settings)
        {
            Settings = settings;
        }

        /// <summary>
        /// The settings instance to use to store an existing container instance.
        /// </summary>
        public SettingsHolder Settings { get; }
    }
}