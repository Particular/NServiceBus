namespace NServiceBus
{
    using Features;

    public static class ConfigureTimeoutManager
    {
        /// <summary>
        /// As Timeout manager is turned on by default for server roles, use DisableTimeoutManager method to turn off Timeout manager
        /// </summary>
        public static Configure DisableTimeoutManager(this Configure config)
        {
            Feature.Disable<TimeoutManager>();
            return config;
        }

    }
}