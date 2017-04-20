namespace Particular.Licensing
{
    using System.IO;

    class LicenseSourceFilePath : LicenseSource
    {
        public LicenseSourceFilePath(string path)
            : base(path)
        { }

        public override LicenseSourceResult Find(string applicationName)
        {
            if (File.Exists(location))
            {
                return ValidateLicense(NonBlockingReader.ReadAllTextWithoutLocking(location), applicationName);
            }

            return new LicenseSourceResult
            {
                Location = location,
                Result = $"License not found in {location}"
            };
        }
    }
}