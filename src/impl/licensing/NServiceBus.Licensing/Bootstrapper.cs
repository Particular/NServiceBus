using NServiceBus.Config;

namespace NServiceBus.Licensing
{
    public class Bootstrapper : INeedInitialization, IWantToRunWhenConfigurationIsComplete
    {
        public void Run()
        {
            var license = Configure.Instance.Builder.Build<LicenseManager>();
            license.Validate();
        }

        public void Init()
        {
            Configure.Instance.Configurer.RegisterSingleton<LicenseManager>(new LicenseManager());
        }
    }
}