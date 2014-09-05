#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;

    [ObsoleteEx(
        RemoveInVersion = "6",
        TreatAsErrorFromVersion = "5")]
    public static class ConfigureSettingLocalAddressNameAction
    {
        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Message = "Queue name is controlled by the endpoint name. The endpoint name can be configured using a EndpointNameAttribute, by passing a serviceName parameter to the host or calling BusConfiguration.EndpointName in the fluent API")]
        public static Configure DefineLocalAddressNameFunc(this Configure config, Func<string> setLocalAddressNameFunc)
        {
            throw new NotImplementedException();
        }
    }
}

