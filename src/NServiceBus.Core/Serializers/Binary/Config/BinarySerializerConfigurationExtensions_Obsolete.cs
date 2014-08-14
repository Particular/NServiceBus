#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global

namespace NServiceBus
{
    using System;
    using Settings;

    [ObsoleteEx(Replacement = "Configure.With(b => b.UseSerialization<Binary>())", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
    public static class BinarySerializerConfigurationExtensions
    {
        [ObsoleteEx(Replacement = "Configure.With(b => b.UseSerialization<Binary>())", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure Binary(this SerializationSettings settings)
        {
            throw new NotImplementedException();
        }
    }
}