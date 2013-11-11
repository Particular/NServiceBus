namespace NServiceBus.Licensing
{
    static class LicenseExpiredFormDisplayer
    {
        public static License PromptUserForLicense()
        {
            using (var form = new LicenseExpiredForm())
            {
                form.ShowDialog();
                if (form.ResultingLicenseText == null)
                {
                    return LicenseDeserializer.GetBasicLicense();
                }
                LicenseLocationConventions.StoreLicenseInRegistry(form.ResultingLicenseText);
                return LicenseDeserializer.Deserialize(form.ResultingLicenseText);
            }

        }
    }
}