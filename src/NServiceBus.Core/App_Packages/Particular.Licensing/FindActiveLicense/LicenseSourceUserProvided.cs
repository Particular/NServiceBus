namespace Particular.Licensing
{
    class LicenseSourceUserProvided : LicenseSource
    {
        string licenseText;

        public LicenseSourceUserProvided(string licenseText) : base("User-provided")
        {
            this.licenseText = licenseText;
        }

        public override LicenseSourceResult Find(string applicationName)
        {
            return ValidateLicense(licenseText, applicationName);
        }
    }
}
