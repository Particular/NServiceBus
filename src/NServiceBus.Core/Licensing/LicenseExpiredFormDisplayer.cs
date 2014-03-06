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
                    return null;
                }
                LicenseLocationConventions.StoreLicenseInRegistry(form.ResultingLicenseText);
                return LicenseDeserializer.Deserialize(form.ResultingLicenseText);
            }

        }
    }
}