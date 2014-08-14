#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global

namespace NServiceBus
{
    using System;

    [ObsoleteEx(Replacement = "Configure.With(b => b.UseSerialization<Binary>())", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
    public static class ConfigureBinarySerializer
    {
        [ObsoleteEx(Replacement = "Configure.With(b => b.UseSerialization<Binary>())", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure BinarySerializer(this Configure config)
        {
            throw new NotImplementedException();
        }
    }
}
