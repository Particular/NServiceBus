using System;
using System.Collections.Generic;
using System.Reflection;
using NServiceBus.Config;
using NServiceBus.Hosting.Configuration;
using NServiceBus.Hosting.Profiles;

namespace NServiceBus.Hosting
{
    using Installation;

    public class DynamicHostController : IHost
    {
        private readonly IConfigureThisEndpoint specifier;
        private readonly ConfigManager configManager;
        private readonly ProfileManager profileManager;

        private DynamicEndpointLoader loader;
        private DynamicEndpointProvisioner provisioner;
        private DynamicEndpointRunner runner;
        private DynamicHostMonitor monitor;
        private List<EndpointToHost> runningServices;

        public DynamicHostController(IConfigureThisEndpoint specifier, string[] requestedProfiles, List<Type> defaultProfiles, string endpointName)
        {
            this.specifier = specifier;
            Configure.GetEndpointNameAction = (Func<string>)(() => endpointName);

            var assembliesToScan = new List<Assembly> {GetType().Assembly};

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

            if (!Configure.WithHasBeenCalled())
                Configure.With(GetType().Assembly);

            if (!Configure.BuilderIsConfigured())
                Configure.Instance.DefaultBuilder();

            Configure.Instance.AzureConfigurationSource();
            Configure.Instance.Configurer.ConfigureComponent<DynamicEndpointLoader>(DependencyLifecycle.SingleInstance);
            Configure.Instance.Configurer.ConfigureComponent<DynamicEndpointProvisioner>(DependencyLifecycle.SingleInstance);
            Configure.Instance.Configurer.ConfigureComponent<DynamicEndpointRunner>(DependencyLifecycle.SingleInstance);
            Configure.Instance.Configurer.ConfigureComponent<DynamicHostMonitor>(DependencyLifecycle.SingleInstance);

            var configSection = Configure.GetConfigSection<DynamicHostControllerConfig>() ?? new DynamicHostControllerConfig();

            Configure.Instance.Configurer.ConfigureProperty<DynamicEndpointLoader>(t => t.ConnectionString, configSection.ConnectionString);
            Configure.Instance.Configurer.ConfigureProperty<DynamicEndpointLoader>(t => t.Container, configSection.Container);
            Configure.Instance.Configurer.ConfigureProperty<DynamicEndpointProvisioner>(t => t.LocalResource, configSection.LocalResource);
            Configure.Instance.Configurer.ConfigureProperty<DynamicEndpointProvisioner>(t => t.RecycleRoleOnError, configSection.RecycleRoleOnError);
            Configure.Instance.Configurer.ConfigureProperty<DynamicEndpointRunner>(t => t.RecycleRoleOnError, configSection.RecycleRoleOnError);
            Configure.Instance.Configurer.ConfigureProperty<DynamicEndpointRunner>(t => t.TimeToWaitUntilProcessIsKilled, configSection.TimeToWaitUntilProcessIsKilled);
            Configure.Instance.Configurer.ConfigureProperty<DynamicHostMonitor>(t => t.Interval, configSection.UpdateInterval);

            configManager.ConfigureCustomInitAndStartup();
            profileManager.ActivateProfileHandlers();

            loader = Configure.Instance.Builder.Build<DynamicEndpointLoader>();
            provisioner = Configure.Instance.Builder.Build<DynamicEndpointProvisioner>();
            runner = Configure.Instance.Builder.Build<DynamicEndpointRunner>();

            var endpointsToHost = loader.LoadEndpoints();
            if (endpointsToHost == null) return;

            runningServices = new List<EndpointToHost>(endpointsToHost);

            provisioner.Provision(runningServices);

            runner.Start(runningServices);
            

            if (!configSection.AutoUpdate) return;

            monitor = Configure.Instance.Builder.Build<DynamicHostMonitor>();
            monitor.UpdatedEndpoints += UpdatedEndpoints;
            monitor.NewEndpoints += NewEndpoints;
            monitor.RemovedEndpoints += RemovedEndpoints;
            monitor.Monitor(runningServices);
            monitor.Start();
        }

        public void Stop()
        {
            if (monitor != null)
                monitor.Stop();

            if (runner != null)
                runner.Stop(runningServices);
        }

        public void Install<TEnvironment>(string username) where TEnvironment : IEnvironment
        {
            //todo -yves
        }

        public void UpdatedEndpoints(object sender, EndpointsEventArgs e)
        {
            runner.Stop(e.Endpoints);
            provisioner.Remove(e.Endpoints);
            provisioner.Provision(e.Endpoints);
            runner.Start(e.Endpoints);
        }

        public void NewEndpoints(object sender, EndpointsEventArgs e)
        {
            provisioner.Provision(e.Endpoints);
            runner.Start(e.Endpoints);
            monitor.Monitor(e.Endpoints);
            runningServices.AddRange(e.Endpoints);
        }

        public void RemovedEndpoints(object sender, EndpointsEventArgs e)
        {
            monitor.StopMonitoring(e.Endpoints);
            runner.Stop(e.Endpoints);
            provisioner.Remove(e.Endpoints);
            foreach (var endpoint in e.Endpoints)
                runningServices.Remove(endpoint);
        }
    }
}
