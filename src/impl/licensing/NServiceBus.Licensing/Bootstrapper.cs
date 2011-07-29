using NServiceBus.Config;

namespace NServiceBus.Licensing
{
    public class Bootstrapper : INeedInitialization, IWantToRunWhenConfigurationIsComplete
    {
        public void Run()
        {
            var license = Configure.Instance.Builder.Build<LicenseManager>();
            var validated = license.Validate();

            if (!validated)
            {
                //Note: No need to quit the application, just in case.
            }
        }

        public void Init()
        {
            Configure.Instance.Configurer.RegisterSingleton<LicenseManager>(new LicenseManager());
        }
    }
}