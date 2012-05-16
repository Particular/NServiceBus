using NServiceBus.Licensing;

namespace NServiceBus
{

    public static class ConfigureLicensing
    {
        public static bool HasValidLicense(this Configure config)
        {
            return hasValidLicense.GetValueOrDefault();
        }
        static ConfigureLicensing()
        {
            if (hasValidLicense.HasValue)
                return;

            var license = Configure.Instance.Builder.Build<LicenseManager>();
            hasValidLicense = license.Validate();
        }
        static bool? hasValidLicense;
    }
}