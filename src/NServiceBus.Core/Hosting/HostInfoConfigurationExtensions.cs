namespace NServiceBus
{
    using System;
    using NServiceBus.Hosting;

    /// <summary>
    /// Host info configuration support
    /// </summary>
    public static class HostInfoConfigurationExtensions
    {
        /// <summary>
        /// Allows custom host information to be used
        /// </summary>
        /// <param name="config"></param>
        /// <param name="customHostInformation"></param>
        public static void HostInformation(this BusConfiguration config,HostInformation customHostInformation)
        {
            if (customHostInformation == null)
            {
                throw new ArgumentNullException("customHostInformation");
            }

            config.Settings.Set<HostInformation>(customHostInformation);

            return;
        }
    }
}