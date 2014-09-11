namespace Particular.Licensing
{
    using System;

    static class LicenseExpirationChecker
    {
        public static bool HasLicenseExpired(License license)
        {
            if (license.ExpirationDate.HasValue && HasLicenseDateExpired(license.ExpirationDate.Value))
            {
                return true;
            }


            if (license.UpgradeProtectionExpiration != null)
            {
                var buildTimeStamp = ReleaseDateReader.GetReleaseDate();
                if (buildTimeStamp > license.UpgradeProtectionExpiration)
                {
                    return true;
                }
            }
            return false;
        }

        static bool HasLicenseDateExpired(DateTime licenseDate)
        {
            var oneDayGrace = licenseDate;
            
            if (licenseDate < DateTime.MaxValue.AddDays(-1))
            {
                oneDayGrace = licenseDate.AddDays(1);
            }
            
            return oneDayGrace < DateTime.UtcNow.Date;
        }
    }
}
