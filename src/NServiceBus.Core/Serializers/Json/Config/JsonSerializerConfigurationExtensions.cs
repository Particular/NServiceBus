#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;
    using Settings;

        [ObsoleteEx(Replacement = "Configure.With(b => b.UseSerialization<Json>())", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
    public static class JsonSerializerConfigurationExtensions
    {
        [ObsoleteEx(Replacement = "Configure.With(b => b.UseSerialization<Json>())", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure Json(this SerializationSettings settings)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(Replacement = "Configure.With(b => b.UseSerialization<Bson>())", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure Bson(this SerializationSettings settings)
        {
            throw new NotImplementedException();
        }
    }
}