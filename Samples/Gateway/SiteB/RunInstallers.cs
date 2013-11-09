using NServiceBus;
using NServiceBus.Config;
using NServiceBus.Installation.Environments;

namespace SiteB
{
    class RunInstallers : IWantToRunWhenConfigurationIsComplete
    {
        public void Run()
        {
            //run the installers to  make sure that all queues are created
            Configure.Instance.ForInstallationOn<Windows>().Install();
        }
    }
}