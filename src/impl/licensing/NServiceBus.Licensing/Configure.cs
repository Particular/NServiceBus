namespace NServiceBus.Licensing
{
    using Config;

    class Configure : INeedInitialization 
    {
        public void Init()
        {
            if (!NServiceBus.Configure.Instance.Configurer.HasComponent<LicenseManager>())
            {
                var licenseManager = new LicenseManager();

                NServiceBus.Configure.Instance.Configurer.RegisterSingleton<LicenseManager>(licenseManager);
            }
        }

        
    }
}