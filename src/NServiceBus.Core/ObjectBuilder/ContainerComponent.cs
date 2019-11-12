namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using Container;
    using ObjectBuilder;
    using ObjectBuilder.Common;
    using Settings;

    class ContainerComponent
    {
        public ContainerComponent(SettingsHolder settings)
        {
            this.settings = settings;
        }

        public IConfigureComponents ContainerConfiguration { get; private set; }

        public void UseContainer<T>(Action<ContainerCustomizations> customizations = null) where T : ContainerDefinition, new()
        {
            customizations?.Invoke(new ContainerCustomizations(settings));

            UseContainer(typeof(T));
        }

        public void UseContainer(Type definitionType)
        {
            UseContainer(definitionType.Construct<ContainerDefinition>().CreateContainer(settings));
        }

        public void UseContainer(IContainer container)
        {
            customContainer = container;
        }

        public void AddUserRegistration(Action<IConfigureComponents> registration)
        {
            userRegistrations.Add(registration);
        }

        public void InitializeWithExternallyManagedContainer(IConfigureComponents configureComponents)
        {
            if (internalContainer != null)
            {
                throw new InvalidOperationException("An internally managed container has already been configured using 'EndpointConfiguration.UseContainer'. It is not possible to use both an internally managed container and an externally managed container.");
            }

            ownsContainer = false;

            ContainerConfiguration = configureComponents;

            settings.AddStartupDiagnosticsSection("Container", new
            {
                Type = "external"
            });

            ApplyRegistrations(configureComponents);
        }

        public IBuilder CreateInternalBuilder()
        {
            if (!ownsContainer)
            {
                throw new InvalidOperationException("An externally managed container has already been configured. It is not possible to use both an internally managed container and an externally managed container.");
            }

            return internalContainer;
        }

        public void InitializeWithInternallyManagedContainer()
        {
            ownsContainer = true;

            var container = customContainer;

            if (container == null)
            {
                settings.AddStartupDiagnosticsSection("Container", new
                {
                    Type = "internal"
                });

                container = new LightInjectObjectBuilder();
            }
            else
            {
                var containerType = container.GetType();

                settings.AddStartupDiagnosticsSection("Container", new
                {
                    Type = containerType.FullName,
                    Version = FileVersionRetriever.GetFileVersion(containerType)
                });
            }

            internalContainer = new CommonObjectBuilder(container);

            ContainerConfiguration = internalContainer;

            ApplyRegistrations(internalContainer);

            //for backwards compatibility we need to make the IBuilder available in the container
            ContainerConfiguration.ConfigureComponent<IBuilder>(_ => internalContainer, DependencyLifecycle.SingleInstance);
        }

        public void Stop()
        {
            if (ownsContainer)
            {
                internalContainer.Dispose();
            }
        }

        void ApplyRegistrations(IConfigureComponents containerConfiguration)
        {
            foreach (var registration in userRegistrations)
            {
                registration(containerConfiguration);
            }
        }

        bool ownsContainer;
        IContainer customContainer;
        CommonObjectBuilder internalContainer;
        List<Action<IConfigureComponents>> userRegistrations = new List<Action<IConfigureComponents>>();
        SettingsHolder settings;
    }
}
