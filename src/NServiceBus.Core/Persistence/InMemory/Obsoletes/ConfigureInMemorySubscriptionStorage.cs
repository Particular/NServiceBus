#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global

namespace NServiceBus
{
    using System;

    [ObsoleteEx(
        RemoveInVersion = "6", 
        TreatAsErrorFromVersion = "5.0",
        Message = "Use `configuration.UsePersistence<InMemoryPersistence>()`, where configuration is an instance of type `BusConfiguration`.")]
    public static class ConfigureInMemorySubscriptionStorage
    {
        [ObsoleteEx(
            RemoveInVersion = "6", 
            TreatAsErrorFromVersion = "5.0", 
            Message = "Use `configuration.UsePersistence<InMemoryPersistence>()`, where configuration is an instance of type `BusConfiguration`.")]
        public static Configure InMemorySubscriptionStorage(this Configure config)
        {
            throw new InvalidOperationException();
        }
    }
}