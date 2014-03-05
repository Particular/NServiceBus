namespace NServiceBus.Licensing
{
    using System;

    static class LicenseExpirationChecker
    {
        public static bool HasLicenseExpired(License license, out string expirationReason)
        {
            if (HasLicenseDateExpired(license.ExpirationDate))
            {
                expirationReason = "Your license has expired.";
                return true;
            }
            if (license.UpgradeProtectionExpiration != null)
            {
                var buildTimeStamp = TimestampReader.GetBuildTimestamp();
                if (buildTimeStamp > license.UpgradeProtectionExpiration)
                {
                    expirationReason = "Your upgrade protection does not cover this version of NServiceBus.";
                    return true;
                }
            }
            expirationReason = null;
            return false;
        }

        public static bool HasLicenseDateExpired(DateTime licenseDate)
        {
            var oneDayGrace = licenseDate >= DateTime.MaxValue.Date ? licenseDate : licenseDate.AddDays(1);
            return oneDayGrace < DateTime.UtcNow.Date;
        }
    }
}