#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;

    public static partial class ConfigureLicenseExtensions
    {

        [ObsoleteEx(Replacement = "Configure.With(c=>c.License(licenseText))", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure License(this Configure config, string licenseText)
        {
            throw new NotImplementedException();
        }


        [ObsoleteEx(Replacement = "Configure.With(c=>c.LicensePath(licenseFile))", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure LicensePath(this Configure config, string licenseFile)
        {
            throw new NotImplementedException();
        }
    }
}
