#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;

    [ObsoleteEx(
        RemoveInVersion = "6.0", 
        TreatAsErrorFromVersion = "5.0")]
    public static class ConfigureExtensions
    {
        [ObsoleteEx(
            Replacement = "Bus.CreateSendOnly(new BusConfiguration())", 
            RemoveInVersion = "6.0", 
            TreatAsErrorFromVersion = "5.0")]
        public static IBus SendOnly(this Configure config)
        {
            throw new InvalidOperationException();
        }
    }
}
