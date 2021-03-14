namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using LightInject;
    using LightInject.Microsoft.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection;
    using Settings;

    class HostCreator
    {
        public static async Task<ExternallyManagedContainerHost> CreateWithExternallyManagedContainer(EndpointConfiguration endpointConfiguration, IServiceCollection serviceCollection, CancellationToken cancellationToken = default)
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

            var endpointCreator = await EndpointCreator.Create(settings, hostingConfiguration, cancellationToken).ConfigureAwait(false);
            var hostingComponent = HostingComponent.Initialize(hostingConfiguration, serviceCollection, false);
            var externallyManagedContainerHost = new ExternallyManagedContainerHost(endpointCreator, hostingComponent);

            return externallyManagedContainerHost;
        }

        public static async Task<IStartableEndpoint> CreateWithInternallyManagedContainer(EndpointConfiguration endpointConfiguration, CancellationToken cancellationToken = default)
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

            var endpointCreator = await EndpointCreator.Create(settings, hostingConfiguration, cancellationToken).ConfigureAwait(false);

            var hostingComponent = HostingComponent.Initialize(hostingConfiguration, serviceCollection, true);

            var containerOptions = new ContainerOptions
            {
                EnableVariance = false
            }.WithMicrosoftSettings();
            var serviceProvider = serviceCollection.CreateLightInjectServiceProvider(containerOptions);
            var startableEndpoint = endpointCreator.CreateStartableEndpoint(serviceProvider, hostingComponent);
            hostingComponent.RegisterBuilder(serviceProvider);

            await hostingComponent.RunInstallers(cancellationToken).ConfigureAwait(false);

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
