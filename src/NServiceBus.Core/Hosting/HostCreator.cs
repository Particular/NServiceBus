namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Extensions.DependencyInjection;
    using LightInject;
    using LightInject.Microsoft.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection;
    using ObjectBuilder;
    using Settings;

    class HostCreator
    {
        public static ExternallyManagedContainerHost CreateWithExternallyManagedContainer(EndpointConfiguration endpointConfiguration, IConfigureComponents externalContainer)
        {
            var settings = endpointConfiguration.Settings;

            CheckIfSettingsWhereUsedToCreateAnotherEndpoint(settings);

            var assemblyScanningComponent = AssemblyScanningComponent.Initialize(settings.Get<AssemblyScanningComponent.Configuration>(), settings);

            endpointConfiguration.FinalizeConfiguration(assemblyScanningComponent.AvailableTypes);

            var hostingSettings = settings.Get<HostingComponent.Settings>();

            var hostingConfiguration = HostingComponent.PrepareConfiguration(hostingSettings, assemblyScanningComponent, externalContainer);

            hostingConfiguration.AddStartupDiagnosticsSection("Container", new
            {
                Type = "external"
            });

            var endpointCreator = EndpointCreator.Create(settings, hostingConfiguration);

            var externallyManagedContainerHost = new ExternallyManagedContainerHost(endpointCreator, hostingConfiguration);

            //for backwards compatibility we need to make the IBuilder available in the container
            externalContainer.ConfigureComponent(_ => externallyManagedContainerHost.Builder.Value, DependencyLifecycle.SingleInstance);

            return externallyManagedContainerHost;
        }

        public static async Task<IStartableEndpoint> CreateWithInternallyManagedContainer(EndpointConfiguration endpointConfiguration)
        {
            var settings = endpointConfiguration.Settings;

            CheckIfSettingsWhereUsedToCreateAnotherEndpoint(settings);

            var assemblyScanningComponent = AssemblyScanningComponent.Initialize(settings.Get<AssemblyScanningComponent.Configuration>(), settings);

            endpointConfiguration.FinalizeConfiguration(assemblyScanningComponent.AvailableTypes);

            var serviceCollection = new ServiceCollection();
            var commonObjectBuilder = new CommonObjectBuilder(serviceCollection);

            IConfigureComponents internalContainer = commonObjectBuilder;
            
            //TODO still needed?
            ////for backwards compatibility we need to make the IBuilder available in the container
            //internalContainer.ConfigureComponent(_ => internalBuilder, DependencyLifecycle.SingleInstance);

            var hostingConfiguration = HostingComponent.PrepareConfiguration(settings.Get<HostingComponent.Settings>(), assemblyScanningComponent, internalContainer);

            hostingConfiguration.AddStartupDiagnosticsSection("Container", new
            {
                Type = "internal"
            });

            var endpointCreator = EndpointCreator.Create(settings, hostingConfiguration);

            var hostingComponent = HostingComponent.Initialize(hostingConfiguration);

            var containerOptions = new ContainerOptions
            {
                EnableVariance = false
            }.WithMicrosoftSettings();
            var serviceProvider = serviceCollection.CreateLightInjectServiceProvider(containerOptions);
            var startableEndpoint = endpointCreator.CreateStartableEndpoint(serviceProvider, hostingComponent);

            hostingComponent.RegisterBuilder(serviceProvider, true);

            await hostingComponent.RunInstallers().ConfigureAwait(false);

            return new InternallyManagedContainerHost(startableEndpoint, hostingComponent);
        }

        static void CheckIfSettingsWhereUsedToCreateAnotherEndpoint(SettingsHolder settings)
        {
            if (settings.GetOrDefault<bool>("UsedToCreateEndpoint"))
            {
                throw new ArgumentException("This EndpointConfiguration was already used for starting an endpoint. Each endpoint requires a new EndpointConfiguration.");
            }

            settings.Set("UsedToCreateEndpoint", true);
        }
    }
}
