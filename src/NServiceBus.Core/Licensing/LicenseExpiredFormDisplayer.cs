namespace NServiceBus.Licensing
{
    using System.Threading;

    static class LicenseExpiredFormDisplayer
    {
        public static License PromptUserForLicense()
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
                    LicenseLocationConventions.StoreLicenseInRegistry(form.ResultingLicenseText);
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