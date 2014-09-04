#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;

    public static partial class ConfigureLicenseExtensions
    {

        [ObsoleteEx(Replacement = "Use configuration.License(licenseText), where configuration is an instance of type BusConfiguration", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure License(this Configure config, string licenseText)
        {
            throw new NotImplementedException();
        }


        [ObsoleteEx(Replacement = "Use configuration.LicensePath(licenseFile), where configuration is an instance of type BusConfiguration", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure LicensePath(this Configure config, string licenseFile)
        {
            throw new NotImplementedException();
        }
    }
}
