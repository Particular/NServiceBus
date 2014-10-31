namespace NServiceBus
{
    using System;
    using SecondLevelRetries.Config;

    /// <summary>
    /// Provides config options for the SLR feature
    /// </summary>
    public static class SecondLevelRetriesConfigExtensions
    {
        /// <summary>
        /// Allows for customization of the second level retries
        /// </summary>
        [ObsoleteEx(
            Message = "Use `configuration.SecondLevelRetries().CustomRetryPolicy()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces.", 
            RemoveInVersion = "6.0", 
            TreatAsErrorFromVersion = "5.0")]
// ReSharper disable UnusedParameter.Global
        public static Configure SecondLevelRetries(this Configure config, Action<SecondLevelRetriesSettings> customSettings)
// ReSharper restore UnusedParameter.Global
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Allows for customization of the second level retries
        /// </summary>
        public static SecondLevelRetriesSettings SecondLevelRetries(this BusConfiguration config)
        {
            return new SecondLevelRetriesSettings(config);
        }
    }
}