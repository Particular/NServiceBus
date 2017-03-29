namespace Particular.Licensing
{
    using System;

    abstract class LicenseSource
    {
        protected string location;

        protected LicenseSource(string location)
        {
            this.location = location;
        }

        public abstract LicenseSourceResult Find(string applicationName);

        protected LicenseSourceResult ValidateLicense(string licenseText, string applicationName)
        {
            if (string.IsNullOrWhiteSpace(applicationName))
            {
                throw new ArgumentException("No application name specified");
            }

            var result = new LicenseSourceResult { Location = location };

            Exception validationFailure;
            if (!LicenseVerifier.TryVerify(licenseText, out validationFailure))
            {
                result.Result = $"License found at '{location}' is not valid - {validationFailure.Message}";
                return result;
            }

            License license;
            try
            {
                license = LicenseDeserializer.Deserialize(licenseText);
            }
            catch
            {
                result.Result = $"License found at '{location}' could not be deserialized";
                return result;
            }

            if (license.ValidForApplication(applicationName))
            {
                result.License = license;
                result.Result = $"License found at '{location}'";
            }
            else
            {
                result.Result = $"License found at '{location}' was not valid for '{applicationName}'. Valid apps: '{string.Join(",", license.ValidApplications)}'";
            }
            return result;
        }
    }
}
