namespace NServiceBus.Licensing
{
    using System.Reflection;
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

            ChangeRhinoLicensingLogLevelToWarn();
        }

        private static void ChangeRhinoLicensingLogLevelToWarn()
        {
            var rhinoLicensingAssembly = Assembly.GetAssembly(typeof(Rhino.Licensing.LicenseValidator));
            if (rhinoLicensingAssembly == null)
            {
                return;
            }
            
            var rhinoLicensingRepository = log4net.LogManager.GetRepository(rhinoLicensingAssembly);
            if (rhinoLicensingRepository == null)
            {
                return;
            }

            var hier = (log4net.Repository.Hierarchy.Hierarchy)rhinoLicensingRepository;
            var licenseValidatorLogger = hier.GetLogger("Rhino.Licensing.LicenseValidator");
            if (licenseValidatorLogger == null)
            {
                return;
            }

            ((log4net.Repository.Hierarchy.Logger)licenseValidatorLogger).Level = hier.LevelMap["FATAL"];
        }
    }
}