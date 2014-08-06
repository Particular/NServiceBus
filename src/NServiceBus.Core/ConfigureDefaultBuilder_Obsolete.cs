#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;

    [ObsoleteEx(
        RemoveInVersion = "6",
        TreatAsErrorFromVersion = "5",
        Message = "Default builder will be used automatically")]
    public static class ConfigureDefaultBuilder
    {

       [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Message = "Default builder will be used automatically")]
        public static Configure DefaultBuilder(this Configure config)
        {
           throw new NotImplementedException();
        }

    }
}
