namespace NServiceBus.Licensing
{
    using System.Threading;
    using Particular.Licensing;

    static class LicenseExpiredFormDisplayer
    {
        public static Particular.Licensing.License PromptUserForLicense()
        {
            SynchronizationContext synchronizationContext = null;
            try
            {
                synchronizationContext = SynchronizationContext.Current;
                using (var form = new LicenseExpiredForm())
                {
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