#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;

    public static partial class InstallConfigExtensions
    {
        [ObsoleteEx(
            Message = "Use `configuration.EnableInstallers()`, where configuration is an instance of type `BusConfiguration`.", 
            RemoveInVersion = "6.0", 
            TreatAsErrorFromVersion = "5.0")]
        public static Configure EnableInstallers(this Configure config, string username = null)
        {
            throw new InvalidOperationException();
        }
    }
}