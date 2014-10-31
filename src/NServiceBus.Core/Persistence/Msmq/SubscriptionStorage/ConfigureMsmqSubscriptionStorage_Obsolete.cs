#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global

namespace NServiceBus
{
    using System;

    [ObsoleteEx(
        RemoveInVersion = "6", 
        TreatAsErrorFromVersion = "5",
        Message = "Use `configuration.UsePersistence<MsmqPersistence>()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces.")]
    public static class ConfigureMsmqSubscriptionStorage
    {
        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Message = "Use configuration.UsePersistence<MsmqPersistence>(), where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces.")]
        public static Configure MsmqSubscriptionStorage(this Configure config)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Message = "Use `configuration.UsePersistence<MsmqPersistence>()`, where `configuration` is an instance of type `BusConfiguration` and assign the queue name via `MsmqSubscriptionStorageConfig` section.")]
        public static Configure MsmqSubscriptionStorage(this Configure config, string endpointName)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "6", 
            TreatAsErrorFromVersion = "5",
            Message = "Assign the queue name via `MsmqSubscriptionStorageConfig` section.")]
        public static Address Queue { get; set; }
    }
}