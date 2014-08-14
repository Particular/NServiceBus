#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;

    [ObsoleteEx(Replacement = "Configure.With(b => b.UseSerialization<Json>())", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
    public static class ConfigureJsonSerializer
    {
        [ObsoleteEx(Replacement = "Configure.With(b => b.UseSerialization<Json>())", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure JsonSerializer(this Configure config)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(Replacement = "Configure.With(b => b.UseSerialization<Bson>())", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure BsonSerializer(this Configure config)
        {
            throw new NotImplementedException();
        }
    }
}