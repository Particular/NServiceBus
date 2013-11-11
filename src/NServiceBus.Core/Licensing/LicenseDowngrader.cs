namespace NServiceBus.Licensing
{
    static class LicenseDowngrader
    {
        public static bool ShouldLicenseDowngrade(License license, out string downgradeReason)
        {
            if (ExpiryChecker.IsExpired(license.ExpirationDate))
            {
                downgradeReason = "Your license has expired.";
                return true;
            }
            if (license.UpgradeProtectionExpiration != null)
            {
                var buildTimeStamp = TimestampReader.GetBuildTimestamp();
                if (buildTimeStamp > license.UpgradeProtectionExpiration)
                {
                    downgradeReason= "Your upgrade protection does not cover this version of NServiceBus.";
                    return true;
                }
            }
            downgradeReason = null;
            return false;
        }
    }
}