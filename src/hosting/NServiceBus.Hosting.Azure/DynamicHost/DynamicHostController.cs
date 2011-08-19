using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using NServiceBus.Config;
using NServiceBus.Hosting.Configuration;
using NServiceBus.Hosting.Helpers;
using NServiceBus.Hosting.Profiles;
using NServiceBus.Integration.Azure;
using NServiceBus.ObjectBuilder;

namespace NServiceBus.Hosting
{
    public class DynamicHostController : IHost
    {
        private IConfigureThisEndpoint specifier;
        private ConfigManager configManager;
        private ProfileManager profileManager;
       
        public DynamicHostController(IConfigureThisEndpoint specifier, string[] requestedProfiles,IEnumerable<Type> defaultProfiles)
        {
            this.specifier = specifier;

            var assembliesToScan = new[] {GetType().Assembly};

            profileManager = new ProfileManager(assembliesToScan, specifier, requestedProfiles, defaultProfiles);
            configManager = new ConfigManager(assembliesToScan, specifier);
        }

        public void Start()
        {
            Configure
                .With(GetType().Assembly)
                .DefaultBuilder()
                .AzureConfigurationSource();

            Configure.Instance.Configurer.ConfigureComponent<DynamicEndpointLoader>(DependencyLifecycle.SingleInstance);
            Configure.Instance.Configurer.ConfigureComponent<DynamicEndpointProvisioner>(DependencyLifecycle.SingleInstance);
            Configure.Instance.Configurer.ConfigureComponent<DynamicEndpointStarter>(DependencyLifecycle.SingleInstance);

            var loader = Configure.Instance.Builder.Build<DynamicEndpointLoader>();
            var provisioner = Configure.Instance.Builder.Build<DynamicEndpointProvisioner>();
            var starter = Configure.Instance.Builder.Build<DynamicEndpointStarter>();

            var endpointsToHost = loader.LoadEndpoints();
            var servicesToRun = provisioner.Provision(endpointsToHost);
            starter.Start(servicesToRun);

            
            //if(IsRunningInAzure())
            //{
            //    // start monitoring the generic host
            //    // only monitor on azure itself and when profile is production
            //}
        }

        public void Stop()
        {
            // stop every host

            // remove the assemblies
        }

        //private bool IsRunningInAzure()
        //{
        //    return RoleEnvironment.IsAvailable && !RoleEnvironment.DeploymentId.StartsWith("deployment(");
        //}

      
    }
}
