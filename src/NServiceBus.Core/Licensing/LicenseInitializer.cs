namespace NServiceBus.Licensing
{
    class LicenseInitializer:INeedInitialization
    {
        public void Init()
        {
            LicenseManager.InitializeLicense();
        }
    }
}