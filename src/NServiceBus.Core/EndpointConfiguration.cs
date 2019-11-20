namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Transactions;
    using Configuration.AdvancedExtensibility;
    using Container;
    using ObjectBuilder;
    using ObjectBuilder.Common;
    using Pipeline;
    using Settings;

    /// <summary>
    /// Configuration used to create an endpoint instance.
    /// </summary>
    public partial class EndpointConfiguration : ExposeSettings
    {
        /// <summary>
        /// Initializes the endpoint configuration builder.
        /// </summary>
        /// <param name="endpointName">The name of the endpoint being configured.</param>
        public EndpointConfiguration(string endpointName)
            : base(new SettingsHolder())
        {
            ValidateEndpointName(endpointName);

            Settings.Set("NServiceBus.Routing.EndpointName", endpointName);

            Settings.SetDefault("Endpoint.SendOnly", false);
            Settings.SetDefault("Transactions.IsolationLevel", IsolationLevel.ReadCommitted);
            Settings.SetDefault("Transactions.DefaultTimeout", TransactionManager.DefaultTimeout);

            Settings.Set(new AssemblyScanningComponent.Configuration(Settings));
            Settings.Set(new InstallationComponent.Configuration(Settings));
            Settings.Set(new HostingComponent.Settings(Settings));
            Settings.Set(new TransportSeam.Settings(Settings));
            Settings.Set(new RoutingComponent.Configuration(Settings));
            Settings.Set(new ReceiveComponent.Settings(Settings));
            Settings.Set(new RecoverabilityComponent.Configuration());
            Settings.Set(Pipeline = new PipelineSettings(Settings));

            Notifications = new Notifications();
            Settings.Set(Notifications);

            ConventionsBuilder = new ConventionsBuilder(Settings);
        }

        /// <summary>
        /// Access to the current endpoint <see cref="Notifications" />.
        /// </summary>
        public Notifications Notifications { get; }

        /// <summary>
        /// Access to the pipeline configuration.
        /// </summary>
        public PipelineSettings Pipeline { get; }

        /// <summary>
        /// Used to configure components in the container.
        /// </summary>
        public void RegisterComponents(Action<IConfigureComponents> registration)
        {
            Guard.AgainstNull(nameof(registration), registration);

            Settings.Get<HostingComponent.Settings>().UserRegistrations.Add(registration);
        }

        /// <summary>
        /// Configures the endpoint to be send-only.
        /// </summary>
        public void SendOnly()
        {
            Settings.Set("Endpoint.SendOnly", true);
        }

        /// <summary>
        /// Defines the conventions to use for this endpoint.
        /// </summary>
        public ConventionsBuilder Conventions()
        {
            return ConventionsBuilder;
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

            Settings.Get<HostingComponent.Settings>().CustomObjectBuilder = definitionType.Construct<ContainerDefinition>().CreateContainer(Settings);
        }

        /// <summary>
        /// Uses an already active instance of a builder.
        /// </summary>
        /// <param name="builder">The instance to use.</param>
        public void UseContainer(IContainer builder)
        {
            Guard.AgainstNull(nameof(builder), builder);

            Settings.Get<HostingComponent.Settings>().CustomObjectBuilder = builder;
        }

        //This needs to be here since we have downstreams that use reflection to access this property
        internal void TypesToScanInternal(IEnumerable<Type> typesToScan)
        {
            Settings.Get<AssemblyScanningComponent.Configuration>().UserProvidedTypes = typesToScan.ToList();
        }

        internal ConventionsBuilder ConventionsBuilder;

        static void ValidateEndpointName(string endpointName)
        {
            if (string.IsNullOrWhiteSpace(endpointName))
            {
                throw new ArgumentException("Endpoint name must not be empty", nameof(endpointName));
            }

            if (endpointName.Contains("@"))
            {
                throw new ArgumentException("Endpoint name must not contain an '@' character.", nameof(endpointName));
            }
        }
    }
}