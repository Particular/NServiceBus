using Microsoft.WindowsAzure.ServiceRuntime;
using NServiceBus.Hosting.Helpers;
using NServiceBus.Integration.Azure;

namespace NServiceBus.Hosting.Azure
{
    /// <summary>
    /// A host implementation for the Azure cloud platform
    /// </summary>
    public class Host : RoleEntryPoint
    {
        private const string ProfileSetting = "NServiceBus.Profile";
        private GenericHost genericHost;

        public override bool OnStart()
        {
            var azureSettings = new AzureConfigurationSettings();
            var requestedProfiles = azureSettings.GetSetting(ProfileSetting);
            var assembliesToScan = AssemblyScanner.GetScannableAssemblies();
            
            IConfigureThisEndpoint specifier = null;
            
            genericHost = new GenericHost(specifier,requestedProfiles.Split(' '), null);

            return true;
        }

        public override void Run()
        {
            genericHost.Start();
        }

        public override void OnStop()
        {
            genericHost.Stop();        
        }
        
    }
}