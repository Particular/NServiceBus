#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;

    [ObsoleteEx(Replacement = "Use configuration.UseSerialization<JsonSerializer>(), where configuration is an instance of type BusConfiguration", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
    public static class ConfigureJsonSerializer
    {
        [ObsoleteEx(Replacement = "Use configuration.UseSerialization<JsonSerializer>(), where configuration is an instance of type BusConfiguration", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure JsonSerializer(this Configure config)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(Replacement = "Use configuration.UseSerialization<BsonSerializer>(), where configuration is an instance of type BusConfiguration", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure BsonSerializer(this Configure config)
        {
            throw new NotImplementedException();
        }
    }
}