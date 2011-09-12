using System;
using System.Collections.Generic;
using NServiceBus.Config;
using NServiceBus.Hosting.Configuration;
using NServiceBus.Hosting.Profiles;
using NServiceBus.ObjectBuilder;

namespace NServiceBus.Hosting
{
    public class DynamicHostController : IHost
    {
        private IConfigureThisEndpoint specifier;
        private ConfigManager configManager;
        private readonly ProfileManager profileManager;
       
        public DynamicHostController(IConfigureThisEndpoint specifier, string[] requestedProfiles,IEnumerable<Type> defaultProfiles)
        {
            this.specifier = specifier;

            var assembliesToScan = new[] {GetType().Assembly};

            profileManager = new ProfileManager(assembliesToScan, specifier, requestedProfiles, defaultProfiles);
            configManager = new ConfigManager(assembliesToScan, specifier);
        }

        public void Start()
        {
            if (specifier is IWantCustomInitialization)
            {
                try
                {
                   (specifier as IWantCustomInitialization).Init();
                }
                catch (NullReferenceException ex)
                {
                    throw new NullReferenceException("NServiceBus has detected a null reference in your initalization code." +
                        " This could be due to trying to use NServiceBus.Configure before it was ready." +
                        " One possible solution is to inherit from IWantCustomInitialization in a different class" +
                        " than the one that inherits from IConfigureThisEndpoint, and put your code there.", ex);
                }
            }

            if (Configure.Instance == null)
                Configure.With(GetType().Assembly);

            if (Configure.Instance.Configurer == null || Configure.Instance.Builder == null)
                Configure.Instance.DefaultBuilder();

            Configure.Instance.AzureConfigurationSource();
            Configure.Instance.Configurer.ConfigureComponent<DynamicEndpointLoader>(DependencyLifecycle.SingleInstance);
            Configure.Instance.Configurer.ConfigureComponent<DynamicEndpointProvisioner>(DependencyLifecycle.SingleInstance);
            Configure.Instance.Configurer.ConfigureComponent<DynamicEndpointStarter>(DependencyLifecycle.SingleInstance);

            configManager.ConfigureCustomInitAndStartup();
            profileManager.ActivateProfileHandlers();

            var loader = Configure.Instance.Builder.Build<DynamicEndpointLoader>();
            var provisioner = Configure.Instance.Builder.Build<DynamicEndpointProvisioner>();
            var starter = Configure.Instance.Builder.Build<DynamicEndpointStarter>();

            var endpointsToHost = loader.LoadEndpoints();
            var servicesToRun = provisioner.Provision(endpointsToHost);
            starter.Start(servicesToRun);
        
        }

        public void Stop()
        {
   
        }
      
    }
}
