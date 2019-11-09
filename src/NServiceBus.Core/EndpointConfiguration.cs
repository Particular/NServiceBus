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
    using Transport;

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

            Settings.Set(new InstallationComponent.Configuration(Settings));
            Settings.Set(new HostingComponent.Configuration(Settings));
            Settings.Set(new TransportComponent.Configuration(Settings));

            Settings.Set("NServiceBus.Routing.EndpointName", endpointName);

            PipelineComponent = new PipelineComponent(Settings);
            ContainerComponent = new ContainerComponent(Settings);

            Settings.Set(new QueueBindings());

            Settings.SetDefault("Endpoint.SendOnly", false);
            Settings.SetDefault("Transactions.IsolationLevel", IsolationLevel.ReadCommitted);
            Settings.SetDefault("Transactions.DefaultTimeout", TransactionManager.DefaultTimeout);

            Notifications = new Notifications();
            Settings.Set(Notifications);
            Settings.Set(new NotificationSubscriptions());

            ConventionsBuilder = new ConventionsBuilder(Settings);
        }

        /// <summary>
        /// Access to the current endpoint <see cref="Notifications" />.
        /// </summary>
        public Notifications Notifications { get; }

        /// <summary>
        /// Access to the pipeline configuration.
        /// </summary>
        public PipelineSettings Pipeline => PipelineComponent.PipelineSettings;

        /// <summary>
        /// Used to configure components in the container.
        /// </summary>
        public void RegisterComponents(Action<IConfigureComponents> registration)
        {
            Guard.AgainstNull(nameof(registration), registration);

            ContainerComponent.AddUserRegistration(registration);
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
            ContainerComponent.UseContainer<T>(customizations);
        }

        /// <summary>
        /// Defines a custom builder to use.
        /// </summary>
        /// <param name="definitionType">The type of the <see cref="ContainerDefinition" />.</param>
        public void UseContainer(Type definitionType)
        {
            Guard.AgainstNull(nameof(definitionType), definitionType);
            Guard.TypeHasDefaultConstructor(definitionType, nameof(definitionType));

            ContainerComponent.UseContainer(definitionType);
        }

        /// <summary>
        /// Uses an already active instance of a builder.
        /// </summary>
        /// <param name="builder">The instance to use.</param>
        public void UseContainer(IContainer builder)
        {
            Guard.AgainstNull(nameof(builder), builder);

            ContainerComponent.UseContainer(builder);
        }

        /// <summary>
        /// Specifies the range of types that NServiceBus scans for handlers etc.
        /// </summary>
        internal void TypesToScanInternal(IEnumerable<Type> typesToScan)
        {
            ScannedTypes = typesToScan.ToList();
        }

        internal ContainerComponent ContainerComponent;
        internal ConventionsBuilder ConventionsBuilder;
        internal PipelineComponent PipelineComponent;
        internal List<Type> ScannedTypes;

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