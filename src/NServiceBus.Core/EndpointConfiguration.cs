namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Transactions;
    using System.Web;
    using Config.ConfigurationSource;
    using Configuration.AdvanceExtensibility;
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

            Settings.Set("NServiceBus.Routing.EndpointName", endpointName);

            Settings.SetDefault<IConfigurationSource>(new DefaultConfigurationSource());

            pipelineCollection = new PipelineConfiguration();
            Settings.Set<PipelineConfiguration>(pipelineCollection);
            Settings.Set<SatelliteDefinitions>(new SatelliteDefinitions());

            Pipeline = new PipelineSettings(pipelineCollection.Modifications, Settings);

            Settings.Set<QueueBindings>(new QueueBindings());

            Settings.SetDefault("Endpoint.SendOnly", false);
            Settings.SetDefault("Transactions.IsolationLevel", IsolationLevel.ReadCommitted);
            Settings.SetDefault("Transactions.DefaultTimeout", TransactionManager.DefaultTimeout);

            Notifications = new Notifications();
            Settings.Set<Notifications>(Notifications);
            Settings.Set<NotificationSubscriptions>(new NotificationSubscriptions());

            conventionsBuilder = new ConventionsBuilder(Settings);
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
            registrations.Add(registration);
        }

        /// <summary>
        /// Append a list of <see cref="Assembly" />s to the ignored list. The string is the file name of the assembly.
        /// </summary>
        [ObsoleteEx(
            Message = "Use the AssemblyScanner configuration API.",
            ReplacementTypeOrMember = "AssemblyScannerConfigurationExtensions.AssemblyScanner",
            TreatAsErrorFromVersion = "7.0",
            RemoveInVersion = "8.0")]
        public void ExcludeAssemblies(params string[] assemblies)
        {
            Settings.GetOrCreate<AssemblyScannerConfiguration>().ExcludeAssemblies(assemblies);
        }

        /// <summary>
        /// Append a list of <see cref="Type" />s to the ignored list.
        /// </summary>
        [ObsoleteEx(
            Message = "Use the AssemblyScanner configuration API.",
            ReplacementTypeOrMember = "AssemblyScannerConfigurationExtensions.AssemblyScanner",
            TreatAsErrorFromVersion = "7.0",
            RemoveInVersion = "8.0")]
        public void ExcludeTypes(params Type[] types)
        {
            Settings.GetOrCreate<AssemblyScannerConfiguration>().ExcludeTypes(types);
        }

        /// <summary>
        /// Specify to scan nested directories when performing assembly scanning.
        /// </summary>
        [ObsoleteEx(
            Message = "Use the AssemblyScanner configuration API.",
            ReplacementTypeOrMember = "AssemblyScannerConfigurationExtensions.AssemblyScanner",
            TreatAsErrorFromVersion = "7.0",
            RemoveInVersion = "8.0")]
        public void ScanAssembliesInNestedDirectories()
        {
            Settings.GetOrCreate<AssemblyScannerConfiguration>().ScanAssembliesInNestedDirectories = true;
        }

        /// <summary>
        /// Configures the endpoint to be send-only.
        /// </summary>
        public void SendOnly()
        {
            Settings.Set("Endpoint.SendOnly", true);
        }

        /// <summary>
        /// Overrides the default configuration source.
        /// </summary>
        public void CustomConfigurationSource(IConfigurationSource configurationSource)
        {
            Guard.AgainstNull(nameof(configurationSource), configurationSource);
            Settings.Set<IConfigurationSource>(configurationSource);
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

        /// <summary>
        /// Specifies the range of types that NServiceBus scans for handlers etc.
        /// </summary>
        internal void TypesToScanInternal(IEnumerable<Type> typesToScan)
        {
            scannedTypes = typesToScan.ToList();
        }

        /// <summary>
        /// Creates the configuration object.
        /// </summary>
        internal InitializableEndpoint Build()
        {
            if (scannedTypes == null)
            {
                var directoryToScan = AppDomain.CurrentDomain.BaseDirectory;
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (HttpRuntime.AppDomainAppId != null)
                {
                    directoryToScan = HttpRuntime.BinDirectory;
                }

                scannedTypes = GetAllowedTypes(directoryToScan);
            }
            else
            {
                scannedTypes = scannedTypes.Union(GetAllowedCoreTypes()).ToList();
            }

            Settings.SetDefault("TypesToScan", scannedTypes);
            ActivateAndInvoke<INeedInitialization>(scannedTypes, t => t.Customize(this));

            UseTransportExtensions.EnsureTransportConfigured(this);
            var container = customBuilder ?? new AutofacObjectBuilder();

            var conventions = conventionsBuilder.Conventions;
            Settings.SetDefault<Conventions>(conventions);
            var messageMetadataRegistry = new MessageMetadataRegistry(conventions);
            messageMetadataRegistry.RegisterMessageTypesFoundIn(Settings.GetAvailableTypes());

            Settings.SetDefault<MessageMetadataRegistry>(messageMetadataRegistry);

            return new InitializableEndpoint(Settings, container, registrations, Pipeline, pipelineCollection);
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
            return assemblyScanner
                .GetScannableAssemblies()
                .Types;
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
            return assemblyScanner
                .GetScannableAssemblies()
                .Types;
        }

        ConventionsBuilder conventionsBuilder;
        IContainer customBuilder;
        PipelineConfiguration pipelineCollection;
        List<Action<IConfigureComponents>> registrations = new List<Action<IConfigureComponents>>();
        List<Type> scannedTypes;
    }
}