namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Container;
    using ObjectBuilder;
    using ObjectBuilder.Common;

    /// <summary>
    /// Configuration used to create an endpoint instance.
    /// </summary>
    public partial class EndpointConfiguration : EndpointConfigurationBase
    {
        /// <summary>
        /// Initializes the endpoint configuration builder.
        /// </summary>
        /// <param name="endpointName">The name of the endpoint being configured.</param>
        public EndpointConfiguration(string endpointName)
            : base(endpointName)
        {
        }

        /// <summary>
        /// Used to configure components in the container.
        /// </summary>
        public void RegisterComponents(Action<IConfigureComponents> registration)
        {
            Guard.AgainstNull(nameof(registration), registration);
            registrations.Add(registration);
        }
        
        /// <summary>
        /// Defines a custom builder to use.
        /// </summary>
        /// <typeparam name="T">The builder type of the <see cref="ContainerDefinition" />.</typeparam>
        public void UseContainer<T>(Action<ContainerCustomizations> customizations = null) where T : ContainerDefinition, new()
        {
            customizations?.Invoke(new ContainerCustomizations(Settings));

            UseContainer(typeof(T));
        }

        /// <summary>
        /// Defines a custom builder to use.
        /// </summary>
        /// <param name="definitionType">The type of the <see cref="ContainerDefinition" />.</param>
        public void UseContainer(Type definitionType)
        {
            Guard.AgainstNull(nameof(definitionType), definitionType);
            Guard.TypeHasDefaultConstructor(definitionType, nameof(definitionType));

            UseContainer(definitionType.Construct<ContainerDefinition>().CreateContainer(Settings));
        }

        /// <summary>
        /// Uses an already active instance of a builder.
        /// </summary>
        /// <param name="builder">The instance to use.</param>
        public void UseContainer(IContainer builder)
        {
            Guard.AgainstNull(nameof(builder), builder);
            customBuilder = builder;
        }

        internal override void TypesScanned(List<Type> scannedTypes)
        {
            base.TypesScanned(scannedTypes);
            ActivateAndInvoke<INeedInitialization>(scannedTypes, t => t.Customize(this));
        }

        internal IInstallableEndpoint Configure()
        {
            var container = ConfigureContainer();
            var containerAdapter = new CommonObjectBuilder(container);

            var configurable = CreateConfigurable(containerAdapter);

            RunUserRegistrations(containerAdapter);

            var configured = configurable.Configure();
            return configured.UseBuilder(containerAdapter);
        }

        void RunUserRegistrations(IConfigureComponents configurator)
        {
            foreach (var registration in registrations)
            {
                registration(configurator);
            }
        }

        IContainer ConfigureContainer()
        {
            if (customBuilder == null)
            {
                Settings.AddStartupDiagnosticsSection("Container", new
                {
                    Type = "internal"
                });
                return new LightInjectObjectBuilder();
            }

            var containerType = customBuilder.GetType();

            Settings.AddStartupDiagnosticsSection("Container", new
            {
                Type = containerType.FullName,
                Version = FileVersionRetriever.GetFileVersion(containerType)
            });

            return customBuilder;
        }
        
        static void ForAllTypes<T>(IEnumerable<Type> types, Action<Type> action) where T : class
        {
            // ReSharper disable HeapView.SlowDelegateCreation
            foreach (var type in types.Where(t => typeof(T).IsAssignableFrom(t) && !(t.IsAbstract || t.IsInterface)))
            {
                action(type);
            }
            // ReSharper restore HeapView.SlowDelegateCreation
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
        List<Action<IConfigureComponents>> registrations = new List<Action<IConfigureComponents>>();

        IContainer customBuilder;
    }
}