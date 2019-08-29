using System;
using System.Collections.Generic;
using NServiceBus.Container;
using NServiceBus.ObjectBuilder;
using NServiceBus.ObjectBuilder.Common;
using NServiceBus.Settings;

namespace NServiceBus
{
    class ContainerComponent
    {
        public ContainerComponent(SettingsHolder settings)
        {
            this.settings = settings;
        }

        public void UseContainer<T>(Action<ContainerCustomizations> customizations = null) where T : ContainerDefinition, new()
        {
            customizations?.Invoke(new ContainerCustomizations(settings));

            UseContainer(typeof(T));
        }

        public void UseContainer(Type definitionType)
        {
            UseContainer(definitionType.Construct<ContainerDefinition>().CreateContainer(settings));
        }

        public void UseExternallyManagedContainer(IConfigureComponents configureComponents)
        {
            if (customContainer != null)
            {
                throw new InvalidOperationException("Configuring an explicit internal container via the `EndpointConfiguration.UseContainer` API is not supported when using an external container and can safely be removed.");
            }

            ContainerConfiguration = configureComponents;
        }

        public void UseExternallyManagedBuilder(IBuilder builder)
        {
            Builder = builder;
        }

        public void UseContainer(IContainer container)
        {
            customContainer = container;
        }

        public void AddUserRegistration(Action<IConfigureComponents> registration)
        {
            userRegistrations.Add(registration);
        }

        public void Initialize()
        {
            if (ContainerConfiguration == null)
            {
                //use internal container
                var container = customContainer;

                if (container == null)
                {
                    settings.AddStartupDiagnosticsSection("Container", new
                    {
                        Type = "internal"
                    });
                    container = new LightInjectObjectBuilder();
                }

                var containerType = container.GetType();

                settings.AddStartupDiagnosticsSection("Container", new
                {
                    Type = containerType.FullName,
                    Version = FileVersionRetriever.GetFileVersion(containerType)
                });

                var commonObjectBuilder = new CommonObjectBuilder(container);

                ContainerConfiguration = commonObjectBuilder;
                Builder = commonObjectBuilder;
            }

            foreach (var registration in userRegistrations)
            {
                registration(ContainerConfiguration);
            }

            //for backwards compatibility we need to make the IBuilder available in the container
            ContainerConfiguration.ConfigureComponent(_ => Builder, DependencyLifecycle.SingleInstance);
        }

        public IConfigureComponents ContainerConfiguration { get; private set; }

        public IBuilder Builder { get; private set; }

        IContainer customContainer;
        List<Action<IConfigureComponents>> userRegistrations = new List<Action<IConfigureComponents>>();
        SettingsHolder settings;
    }
}
