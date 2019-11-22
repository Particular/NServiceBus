namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using ObjectBuilder;

    class HostCreator
    {
        public static StartableEndpointWithExternallyManagedContainer CreateWithExternallyManagedContainer(EndpointConfiguration endpointConfiguration, IConfigureComponents externalContainer)
        {
            var settings = endpointConfiguration.Settings;

            var assemblyScanningComponent = AssemblyScanningComponent.Initialize(settings.Get<AssemblyScanningComponent.Configuration>(), settings);

            var conventions = FinalizeConfiguration(endpointConfiguration, assemblyScanningComponent.AvailableTypes);

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

            var endpointCreator = EndpointCreator.Create(settings, hostingConfiguration, conventions);

            var startableEndpoint = new StartableEndpointWithExternallyManagedContainer(endpointCreator, hostingConfiguration);

            //for backwards compatibility we need to make the IBuilder available in the container
            externalContainer.ConfigureComponent(_ => startableEndpoint.Builder.Value, DependencyLifecycle.SingleInstance);

            return startableEndpoint;
        }

        public static async Task<IStartableEndpoint> CreateWithInternallyManagedContainer(EndpointConfiguration endpointConfiguration)
        {
            var settings = endpointConfiguration.Settings;

            var assemblyScanningComponent = AssemblyScanningComponent.Initialize(settings.Get<AssemblyScanningComponent.Configuration>(), settings);

            var conventions = FinalizeConfiguration(endpointConfiguration, assemblyScanningComponent.AvailableTypes);

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

            var endpointCreator = EndpointCreator.Create(settings, hostingConfiguration, conventions);

            var hostingComponent = HostingComponent.Initialize(hostingConfiguration);

            var startableEndpoint = endpointCreator.CreateStartableEndpoint(internalBuilder, hostingComponent);

            hostingComponent.RegisterBuilder(internalBuilder, true);

            await hostingComponent.RunInstallers().ConfigureAwait(false);

            return new StartableEndpointWithInternallyManagedContainer(startableEndpoint, hostingComponent);
        }

        static Conventions FinalizeConfiguration(EndpointConfiguration endpointConfiguration, List<Type> availableTypes)
        {
            ActivateAndInvoke<INeedInitialization>(availableTypes, t => t.Customize(endpointConfiguration));

            var conventions = endpointConfiguration.ConventionsBuilder.Conventions;
            endpointConfiguration.Settings.SetDefault(conventions);

            return conventions;
        }

        static void ActivateAndInvoke<T>(IList<Type> types, Action<T> action) where T : class
        {
            ForAllTypes<T>(types, t =>
            {
                if (!HasDefaultConstructor(t))
                {
                    throw new Exception($"Unable to create the type '{t.Name}'. Types implementing '{typeof(T).Name}' must have a public parameterless (default) constructor.");
                }

                var instanceToInvoke = (T)Activator.CreateInstance(t);
                action(instanceToInvoke);
            });
        }

        static bool HasDefaultConstructor(Type type) => type.GetConstructor(Type.EmptyTypes) != null;

        static void ForAllTypes<T>(IEnumerable<Type> types, Action<Type> action) where T : class
        {
            // ReSharper disable HeapView.SlowDelegateCreation
            foreach (var type in types.Where(t => typeof(T).IsAssignableFrom(t) && !(t.IsAbstract || t.IsInterface)))
            {
                action(type);
            }
            // ReSharper restore HeapView.SlowDelegateCreation
        }
    }
}
