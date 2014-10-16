#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global

namespace NServiceBus
{
    using System;

    [ObsoleteEx(
        Message = "Use `configuration.UseSerialization<BinarySerializer>()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces.",
        RemoveInVersion = "6.0",
        TreatAsErrorFromVersion = "5.0")]
    public static class ConfigureBinarySerializer
    {
        [ObsoleteEx(
            Message = "Use `configuration.UseSerialization<BinarySerializer>()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces.", 
            RemoveInVersion = "6.0",
            TreatAsErrorFromVersion = "5.0")]
        public static Configure BinarySerializer(this Configure config)
        {
            throw new NotImplementedException();
        }
    }
}
