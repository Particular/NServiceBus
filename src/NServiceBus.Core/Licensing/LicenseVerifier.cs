namespace NServiceBus.Licensing
{

    class LicenseVerifier : IWantToRunBeforeConfiguration
    {
        public void Init()
        {
            LicenseManager.Verify();
        }
    }
}