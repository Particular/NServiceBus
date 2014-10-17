// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;

    public static partial class ConfigurePurging
    {

        /// <summary>
        /// Requests that the incoming queue be purged of all messages when the bus is started.
        /// All messages in this queue will be deleted if this is true.
        /// Setting this to true may make sense for certain smart-client applications, 
        /// but rarely for server applications.
        /// </summary>
        [ObsoleteEx(
            Message = "Use `configuration.PurgeOnStartup()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces.", 
            RemoveInVersion = "6.0", 
            TreatAsErrorFromVersion = "5.0")]
        public static Configure PurgeOnStartup(this Configure config, bool value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// True if the users wants the input queue to be purged when we starts up
        /// </summary>
        [ObsoleteEx(
            Message = "The `ReadOnlySettings` extension method `ConfigurePurging.GetPurgeOnStartup`", 
            RemoveInVersion = "6.0", 
            TreatAsErrorFromVersion = "5.0")]
        public static bool PurgeRequested
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }
}