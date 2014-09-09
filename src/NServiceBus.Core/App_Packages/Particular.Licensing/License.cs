namespace Particular.Licensing
{
    using System;
    using System.Collections.Generic;

    class License
    {
        public static License TrialLicense(DateTime trialStartDate)
        {
            return new License
            {
                LicenseType = "Trial",
                ExpirationDate = trialStartDate.AddDays(14),
                IsExtendedTrial = false,
                ValidApplications = new List<string> { "All"}
            };
        }

        public License()
        {
            ValidApplications = new List<string>();
        }

        public DateTime? ExpirationDate { get; set; }

        public bool IsTrialLicense
        {
            get { return !IsCommercialLicense; }
        }

        public bool IsExtendedTrial { get; set; }

        public bool IsCommercialLicense
        {
            get { return LicenseType.ToLower() != "trial"; }
        }

        public string LicenseType { get; set; }

        public string RegisteredTo { get; set; }

        public DateTime? UpgradeProtectionExpiration { get; internal set; }

        public List<string> ValidApplications{ get; internal set; }

        public bool ValidForApplication(string applicationName)
        {
            return ValidApplications.Contains(applicationName) || ValidApplications.Contains("All");
        }
    }
}