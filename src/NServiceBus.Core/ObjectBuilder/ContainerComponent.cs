﻿using System;
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

        public IConfigureComponents ContainerConfiguration { get; private set; }

        public IBuilder Builder { get; private set; }

        public void UseContainer<T>(Action<ContainerCustomizations> customizations = null) where T : ContainerDefinition, new()
        {
            customizations?.Invoke(new ContainerCustomizations(settings));

            UseContainer(typeof(T));
        }

        public void UseContainer(Type definitionType)
        {
            UseContainer(definitionType.Construct<ContainerDefinition>().CreateContainer(settings));
        }

        public void UseExternallyManagedBuilder(IBuilder builder)
        {
            ownedContainer = false;
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

        public void InitializeWithExternallyManagedContainer(IConfigureComponents configureComponents)
        {
            if (customContainer != null)
            {
                throw new InvalidOperationException("An internally managed container has already been configured using 'EndpointConfiguration.UseContainer'. It is not possible to use both an internally managed container and an externally managed container.");
            }

            ContainerConfiguration = configureComponents;

            settings.AddStartupDiagnosticsSection("Container", new
            {
                Type = "external"
            });

            ApplyRegistrations();
        }

        public void InitializeWithInternallyManagedContainer()
        {
            ownedContainer = true;

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

            var commonObjectBuilder = new CommonObjectBuilder(container);

            ContainerConfiguration = commonObjectBuilder;
            Builder = commonObjectBuilder;

            ApplyRegistrations();
        }

        public void Stop()
        {
            if (ownedContainer)
            {
                Builder.Dispose();
            }
        }

        void ApplyRegistrations()
        {
            foreach (var registration in userRegistrations)
            {
                registration(ContainerConfiguration);
            }

            //for backwards compatibility we need to make the IBuilder available in the container
            ContainerConfiguration.ConfigureComponent(_ => Builder, DependencyLifecycle.SingleInstance);
        }

        bool ownedContainer;
        IContainer customContainer;
        List<Action<IConfigureComponents>> userRegistrations = new List<Action<IConfigureComponents>>();
        SettingsHolder settings;
    }
}
