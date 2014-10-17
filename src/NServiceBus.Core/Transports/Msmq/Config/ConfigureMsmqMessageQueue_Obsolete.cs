#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;

    [ObsoleteEx(Message = "Please use 'UsingTransport<MsmqTransport>' on your 'IConfigureThisEndpoint' class or use `configuration.UseTransport<MsmqTransport>()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces.", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
    public static class ConfigureMsmqMessageQueue
    {
        [ObsoleteEx(Message = "Please use 'UsingTransport<MsmqTransport>' on your 'IConfigureThisEndpoint' class or use `configuration.UseTransport<MsmqTransport>()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces.", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure MsmqTransport(this Configure config)
        {
            throw new NotImplementedException();
        }
    }
}