namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Transactions;
    using Configuration.AdvancedExtensibility;
    using Container;
    using Hosting.Helpers;
    using ObjectBuilder;
    using ObjectBuilder.Common;
    using Pipeline;
    using Settings;
    using Transport;
    using Unicast.Messages;

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

            Settings.Set(new StartupDiagnosticEntries());

            Settings.Set("NServiceBus.Routing.EndpointName", endpointName);

            pipelineComponent = new PipelineComponent(Settings);
            containerComponent = new ContainerComponent(Settings);

            Settings.Set(new QueueBindings());

            Settings.SetDefault("Endpoint.SendOnly", false);
            Settings.SetDefault("Transactions.IsolationLevel", IsolationLevel.ReadCommitted);
            Settings.SetDefault("Transactions.DefaultTimeout", TransactionManager.DefaultTimeout);

            Notifications = new Notifications();
            Settings.Set(Notifications);
            Settings.Set(new NotificationSubscriptions());

            conventionsBuilder = new ConventionsBuilder(Settings);
        }

        /// <summary>
        /// Access to the current endpoint <see cref="Notifications" />.
        /// </summary>
        public Notifications Notifications { get; }

        /// <summary>
        /// Access to the pipeline configuration.
        /// </summary>
        public PipelineSettings Pipeline => pipelineComponent.PipelineSettings;

        /// <summary>
        /// Used to configure components in the container.
        /// </summary>
        public void RegisterComponents(Action<IConfigureComponents> registration)
        {
            Guard.AgainstNull(nameof(registration), registration);

            containerComponent.AddUserRegistration(registration);
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
            return conventionsBuilder;
        }

        /// <summary>
        /// Defines a custom builder to use.
        /// </summary>
        /// <typeparam name="T">The builder type of the <see cref="ContainerDefinition" />.</typeparam>
        public void UseContainer<T>(Action<ContainerCustomizations> customizations = null) where T : ContainerDefinition, new()
        {
            containerComponent.UseContainer<T>(customizations);
        }

        /// <summary>
        /// Defines a custom builder to use.
        /// </summary>
        /// <param name="definitionType">The type of the <see cref="ContainerDefinition" />.</param>
        public void UseContainer(Type definitionType)
        {
            Guard.AgainstNull(nameof(definitionType), definitionType);
            Guard.TypeHasDefaultConstructor(definitionType, nameof(definitionType));

            containerComponent.UseContainer(definitionType);
        }

        /// <summary>
        /// Uses an already active instance of a builder.
        /// </summary>
        /// <param name="builder">The instance to use.</param>
        public void UseContainer(IContainer builder)
        {
            Guard.AgainstNull(nameof(builder), builder);

            containerComponent.UseContainer(builder);
        }

        /// <summary>
        /// Specifies the range of types that NServiceBus scans for handlers etc.
        /// </summary>
        internal void TypesToScanInternal(IEnumerable<Type> typesToScan)
        {
            scannedTypes = typesToScan.ToList();
        }

        internal InitializableEndpoint Build()
        {
            if (scannedTypes == null)
            {
                var directoryToScan = AppDomain.CurrentDomain.RelativeSearchPath ?? AppDomain.CurrentDomain.BaseDirectory;

                scannedTypes = GetAllowedTypes(directoryToScan);
            }
            else
            {
                scannedTypes = scannedTypes.Union(GetAllowedCoreTypes()).ToList();
            }

            Settings.SetDefault("TypesToScan", scannedTypes);
            ActivateAndInvoke<INeedInitialization>(scannedTypes, t => t.Customize(this));

            var conventions = conventionsBuilder.Conventions;
            Settings.SetDefault(conventions);

            ConfigureMessageTypes(conventions);
            containerComponent.Initialize();
            return new InitializableEndpoint(Settings, containerComponent, pipelineComponent);
        }
       
        void ConfigureMessageTypes(Conventions conventions)
        {
            var messageMetadataRegistry = new MessageMetadataRegistry(conventions.IsMessageType);

            messageMetadataRegistry.RegisterMessageTypesFoundIn(Settings.GetAvailableTypes());

            Settings.Set(messageMetadataRegistry);

            var foundMessages = messageMetadataRegistry.GetAllMessages().ToList();

            Settings.AddStartupDiagnosticsSection("Messages", new
            {
                CustomConventionUsed = conventions.CustomMessageTypeConventionUsed,
                NumberOfMessagesFoundAtStartup = foundMessages.Count,
                Messages = foundMessages.Select(m => m.MessageType.FullName)
            });
        }

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

        List<Type> GetAllowedTypes(string path)
        {
            var assemblyScannerSettings = Settings.GetOrCreate<AssemblyScannerConfiguration>();
            var assemblyScanner = new AssemblyScanner(path)
            {
                AssembliesToSkip = assemblyScannerSettings.ExcludedAssemblies,
                TypesToSkip = assemblyScannerSettings.ExcludedTypes,
                ScanNestedDirectories = assemblyScannerSettings.ScanAssembliesInNestedDirectories,
                ThrowExceptions = assemblyScannerSettings.ThrowExceptions,
                ScanAppDomainAssemblies = assemblyScannerSettings.ScanAppDomainAssemblies
            };

            return Scan(assemblyScanner);
        }

        List<Type> GetAllowedCoreTypes()
        {
            var assemblyScannerSettings = Settings.GetOrCreate<AssemblyScannerConfiguration>();
            var assemblyScanner = new AssemblyScanner(Assembly.GetExecutingAssembly())
            {
                AssembliesToSkip = assemblyScannerSettings.ExcludedAssemblies,
                TypesToSkip = assemblyScannerSettings.ExcludedTypes,
                ScanNestedDirectories = assemblyScannerSettings.ScanAssembliesInNestedDirectories,
                ThrowExceptions = assemblyScannerSettings.ThrowExceptions,
                ScanAppDomainAssemblies = assemblyScannerSettings.ScanAppDomainAssemblies
            };

            return Scan(assemblyScanner);
        }

        List<Type> Scan(AssemblyScanner assemblyScanner)
        {
            var results = assemblyScanner.GetScannableAssemblies();

            Settings.AddStartupDiagnosticsSection("AssemblyScanning", new
            {
                Assemblies = results.Assemblies.Select(a => a.FullName),
                results.ErrorsThrownDuringScanning,
                results.SkippedFiles
            });

            return results.Types;
        }

        internal void UseExternallyManagedContainer(IConfigureComponents configureComponents)
        {
            containerComponent.UseExternallyManagedContainer(configureComponents);
        }

        ContainerComponent containerComponent;
        ConventionsBuilder conventionsBuilder;
        PipelineComponent pipelineComponent;
        List<Type> scannedTypes;
    }
}