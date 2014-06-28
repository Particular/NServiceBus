namespace NServiceBus
{
    using System;
    using Settings;


    /// <summary>
    /// Adds an extention point to allow endpoint related changes
    /// </summary>
    public static class EndpointSettingsExtensions
    {
        /// <summary>
        /// Allows users to change endpoint related settings
        /// </summary>
        public static Configure Endpoint(this Configure config,Action<EndpointSettings> customizations)
        {
            customizations(new EndpointSettings(config));

            return config;
        }

    }
}