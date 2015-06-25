namespace NServiceBus
{

    /// <summary>
    /// Contains extension methods to <see cref="BusConfiguration"/>.
    /// </summary>
    public static class ConfigureError
    {

        /// <summary>
        /// Configure error queue settings. 
        /// </summary>
        /// <param name="config">The <see cref="BusConfiguration"/> instance to apply the settings to.</param>
        /// <param name="errorQueue">The name of the error queue to use.</param>
        public static void SendFailedMessagesTo(this BusConfiguration config, string errorQueue)
        {
            Guard.AgainstNull(config, "config");
            Guard.AgainstNullAndEmpty(errorQueue, "errorQueue");
            config.Settings.Set("errorQueue", errorQueue);
        }
    }
}