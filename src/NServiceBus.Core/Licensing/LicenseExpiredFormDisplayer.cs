namespace NServiceBus.Licensing
{
    using System.Threading;
    using Particular.Licensing;

    static class LicenseExpiredFormDisplayer
    {
        public static License PromptUserForLicense(License currentLicense)
        {
            SynchronizationContext synchronizationContext = null;
            try
            {
                synchronizationContext = SynchronizationContext.Current;
                using (var form = new LicenseExpiredForm())
                {
                    form.CurrentLicense = currentLicense;
                    form.ShowDialog();
                    if (form.ResultingLicenseText == null)
                    {
                        return null;
                    }

                    new RegistryLicenseStore()
                        .StoreLicense(form.ResultingLicenseText);

                    return LicenseDeserializer.Deserialize(form.ResultingLicenseText);
                }

            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(synchronizationContext);
            }
        }
    }
}