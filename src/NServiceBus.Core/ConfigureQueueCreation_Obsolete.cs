#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;

    public static partial class ConfigureQueueCreation
    {

        [ObsoleteEx(
            Message = "Use `configuration.DoNotCreateQueues()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces.", 
            RemoveInVersion = "6.0", 
            TreatAsErrorFromVersion = "5.0")]
        public static Configure DoNotCreateQueues(this Configure config)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static bool DontCreateQueues
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }
}