using System.Reflection;
using NServiceBus.Config;
using NServiceBus.Unicast.Transport.Transactional;

namespace NServiceBus.Licensing
{
    public class Bootstrapper : INeedInitialization, IWantToRunWhenConfigurationIsComplete
    {
        public void Run()
        {
            var validated = Configure.Instance.HasValidLicense();
            if (validated)
            {
                ChangeRhinoLicensingLogLevelToWarn();
                return;
            }

            var transport = Configure.Instance.Builder.Build<TransactionalTransport>();
            var numWorkerThreadsInfo = typeof (TransactionalTransport).GetField("numberOfWorkerThreads",
                                                        BindingFlags.Instance | BindingFlags.NonPublic);

            //intentionally don't check for null so that this will blow up if there are changes
            numWorkerThreadsInfo.SetValue(transport, 1);
        }
        
        private void ChangeRhinoLicensingLogLevelToWarn()
        {
            Assembly rhinoLicensingAssembly = Assembly.GetAssembly(typeof(Rhino.Licensing.LicenseValidator));
            if (rhinoLicensingAssembly == null) return;
            log4net.Repository.ILoggerRepository rhinoLicensingRepository =
                log4net.LogManager.GetRepository(rhinoLicensingAssembly);
            
            if (rhinoLicensingRepository == null) return;

            var hier = (log4net.Repository.Hierarchy.Hierarchy)rhinoLicensingRepository;
            var licenseValidatorLogger = hier.GetLogger("Rhino.Licensing.LicenseValidator");
            if (licenseValidatorLogger == null) return;

            ((log4net.Repository.Hierarchy.Logger)licenseValidatorLogger).Level = hier.LevelMap["WARN"];

        }
        public void Init()
        {
            Configure.Instance.Configurer.RegisterSingleton<LicenseManager>(new LicenseManager());
        }
    }
}