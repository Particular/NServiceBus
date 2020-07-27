namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
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

            if (hostingSettings.CustomObjectBuilder != null)
            {
                throw new InvalidOperationException("An internally managed container has already been configured using 'EndpointConfiguration.UseContainer'. It is not possible to use both an internally managed container and an externally managed container.");
            }

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

            var hostingSetting = settings.Get<HostingComponent.Settings>();
            var useDefaultBuilder = hostingSetting.CustomObjectBuilder == null;
            var container = useDefaultBuilder ? new LightInjectObjectBuilder() : hostingSetting.CustomObjectBuilder;

            var commonObjectBuilder = new CommonObjectBuilder(container);

            IConfigureComponents internalContainer = commonObjectBuilder;
            IBuilder internalBuilder = commonObjectBuilder;

            //for backwards compatibility we need to make the IBuilder available in the container
            internalContainer.ConfigureComponent(_ => internalBuilder, DependencyLifecycle.SingleInstance);

            var hostingConfiguration = HostingComponent.PrepareConfiguration(settings.Get<HostingComponent.Settings>(), assemblyScanningComponent, internalContainer);

            if (useDefaultBuilder)
            {
                hostingConfiguration.AddStartupDiagnosticsSection("Container", new
                {
                    Type = "internal"
                });
            }
            else
            {
                var containerType = internalContainer.GetType();

                hostingConfiguration.AddStartupDiagnosticsSection("Container", new
                {
                    Type = containerType.FullName,
                    Version = FileVersionRetriever.GetFileVersion(containerType)
                });
            }

            var endpointCreator = EndpointCreator.Create(settings, hostingConfiguration);

            var hostingComponent = HostingComponent.Initialize(hostingConfiguration);

            var startableEndpoint = endpointCreator.CreateStartableEndpoint(internalBuilder, hostingComponent);

            hostingComponent.RegisterBuilder(internalBuilder, true);

            await hostingComponent.RunInstallers().ConfigureAwait(false);

            return new InternallyManagedContainerHost(startableEndpoint, hostingComponent);
        }

        static void CheckIfSettingsWhereUsedToCreateAnotherEndpoint(SettingsHolder settings)
        {
            if (settings.GetOrDefault<bool>("UsedToCreateEndpoint"))
            {
                throw new ArgumentException("This EndpointConfiguration was already used for starting an endpoint, create a new one before starting the other.");
            }

            settings.Set("UsedToCreateEndpoint", true);
        }
    }
}
