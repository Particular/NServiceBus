#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;

    [ObsoleteEx(
        RemoveInVersion = "6",
        TreatAsErrorFromVersion = "5",
        Message = "Default builder will be used automatically. It is safe to remove this code.")]
    public static class ConfigureDefaultBuilder
    {

       [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Message = "Default builder will be used automatically. It is safe to remove this code.")]
        public static Configure DefaultBuilder(this Configure config)
        {
           throw new NotImplementedException();
        }

    }
}
