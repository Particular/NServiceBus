#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global

namespace NServiceBus
{
    using System;

    [ObsoleteEx(
        Message = "Use `configuration.UseSerialization<BinarySerializer>()`, where `configuration` is an instance of type `BusConfiguration`.",
        RemoveInVersion = "6.0",
        TreatAsErrorFromVersion = "5.0")]
    public static class ConfigureBinarySerializer
    {
        [ObsoleteEx(
            Message = "Use `configuration.UseSerialization<BinarySerializer>()`, where `configuration` is an instance of type `BusConfiguration`.", 
            RemoveInVersion = "6.0",
            TreatAsErrorFromVersion = "5.0")]
        public static Configure BinarySerializer(this Configure config)
        {
            throw new NotImplementedException();
        }
    }
}
