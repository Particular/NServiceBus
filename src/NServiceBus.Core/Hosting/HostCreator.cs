namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using LightInject;
    using LightInject.Microsoft.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection;
    using ObjectBuilder;
    using Settings;

    class HostCreator
    {
        public static ExternallyManagedContainerHost CreateWithExternallyManagedContainer(EndpointConfiguration endpointConfiguration, IServiceCollection serviceCollection)
        {
            var settings = endpointConfiguration.Settings;

            CheckIfSettingsWhereUsedToCreateAnotherEndpoint(settings);

            var assemblyScanningComponent = AssemblyScanningComponent.Initialize(settings.Get<AssemblyScanningComponent.Configuration>(), settings);

            endpointConfiguration.FinalizeConfiguration(assemblyScanningComponent.AvailableTypes);

            var hostingSettings = settings.Get<HostingComponent.Settings>();

            var hostingConfiguration = HostingComponent.PrepareConfiguration(hostingSettings, assemblyScanningComponent, serviceCollection);

            hostingConfiguration.AddStartupDiagnosticsSection("Container", new
            {
                Type = "external"
            });

            var endpointCreator = EndpointCreator.Create(settings, hostingConfiguration);

            var externallyManagedContainerHost = new ExternallyManagedContainerHost(endpointCreator, hostingConfiguration);

            return externallyManagedContainerHost;
        }

        public static async Task<IStartableEndpoint> CreateWithInternallyManagedContainer(EndpointConfiguration endpointConfiguration)
        {
            var settings = endpointConfiguration.Settings;

            CheckIfSettingsWhereUsedToCreateAnotherEndpoint(settings);

            var assemblyScanningComponent = AssemblyScanningComponent.Initialize(settings.Get<AssemblyScanningComponent.Configuration>(), settings);

            endpointConfiguration.FinalizeConfiguration(assemblyScanningComponent.AvailableTypes);

            var serviceCollection = new MicrosoftExtensionsDependencyInjection.ServiceCollection();

            var hostingConfiguration = HostingComponent.PrepareConfiguration(settings.Get<HostingComponent.Settings>(), assemblyScanningComponent, serviceCollection);

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
