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
    using Routing;
    using Settings;
    using Transports;

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
            Guard.AgainstNullAndEmpty(nameof(endpointName), endpointName);

            Settings.Set<EndpointName>(new EndpointName(endpointName));

            configurationSourceToUse = new DefaultConfigurationSource();

            pipelineCollection = new PipelineConfiguration();
            Settings.Set<PipelineConfiguration>(pipelineCollection);
            Pipeline = new PipelineSettings(pipelineCollection.MainPipeline);

            Settings.Set<QueueBindings>(new QueueBindings());

            Settings.SetDefault("Endpoint.SendOnly", false);
            Settings.SetDefault("Transactions.IsolationLevel", IsolationLevel.ReadCommitted);
            Settings.SetDefault("Transactions.DefaultTimeout", TransactionManager.DefaultTimeout);

            Settings.Set<Notifications>(new Notifications());
            Settings.Set<NotificationSubscriptions>(new NotificationSubscriptions());

            conventionsBuilder = new ConventionsBuilder(Settings);
        }

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
        public void ExcludeAssemblies(params string[] assemblies)
        {
            Guard.AgainstNull(nameof(assemblies), assemblies);

            if (assemblies.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException("Passed in a null or empty assembly name.", nameof(assemblies));
            }
            excludedAssemblies = excludedAssemblies.Union(assemblies, StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        /// Append a list of <see cref="Type" />s to the ignored list.
        /// </summary>
        public void ExcludeTypes(params Type[] types)
        {
            Guard.AgainstNull(nameof(types), types);
            if (types.Any(x => x == null))
            {
                throw new ArgumentException("Passed in a null or empty type.", nameof(types));
            }

            excludedTypes = excludedTypes.Union(types).ToList();
        }

        /// <summary>
        /// Specify to scan nested directories when performing assembly scanning.
        /// </summary>
        public void ScanAssembliesInNestedDirectories()
        {
            scanAssembliesInNestedDirectories = true;
        }

        /// <summary>
        /// Configures the endpoint to be send-only.
        /// </summary>
        public void SendOnly()
        {
            sendOnly = true;
        }

        /// <summary>
        /// Overrides the default configuration source.
        /// </summary>
        public void CustomConfigurationSource(IConfigurationSource configurationSource)
        {
            Guard.AgainstNull(nameof(configurationSource), configurationSource);
            configurationSourceToUse = configurationSource;
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
        /// Sets the public return address of this endpoint.
        /// </summary>
        /// <param name="address">The public address.</param>
        public void OverridePublicReturnAddress(string address)
        {
            Guard.AgainstNullAndEmpty(nameof(address), address);
            publicReturnAddress = address;
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
            Settings.Set("Endpoint.SendOnly", sendOnly);
            ActivateAndInvoke<INeedInitialization>(scannedTypes, t => t.Customize(this));

            UseTransportExtensions.EnsureTransportConfigured(this);
            var container = customBuilder ?? new AutofacObjectBuilder();

            Settings.SetDefault<IConfigurationSource>(configurationSourceToUse);

            if (publicReturnAddress != null)
            {
                Settings.SetDefault("PublicReturnAddress", publicReturnAddress);
            }

            Settings.SetDefault<Conventions>(conventionsBuilder.Conventions);

            return new InitializableEndpoint(Settings, container, registrations, Pipeline, pipelineCollection);
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

                var instanceToInvoke = (T) Activator.CreateInstance(t);
                action(instanceToInvoke);
            });
        }

        static bool HasDefaultConstructor(Type type) => type.GetConstructor(Type.EmptyTypes) != null;

        List<Type> GetAllowedTypes(string path)
        {
            var assemblyScanner = new AssemblyScanner(path)
            {
                AssembliesToSkip = excludedAssemblies,
                TypesToSkip = excludedTypes,
                ScanNestedDirectories = scanAssembliesInNestedDirectories
            };
            return assemblyScanner
                .GetScannableAssemblies()
                .Types;
        }

        List<Type> GetAllowedCoreTypes()
        {
            var assemblyScanner = new AssemblyScanner(Assembly.GetExecutingAssembly())
            {
                TypesToSkip = excludedTypes,
                ScanNestedDirectories = scanAssembliesInNestedDirectories
            };
            return assemblyScanner
                .GetScannableAssemblies()
                .Types;
        }

        IConfigurationSource configurationSourceToUse;
        ConventionsBuilder conventionsBuilder;
        IContainer customBuilder;
        List<string> excludedAssemblies = new List<string>();
        List<Type> excludedTypes = new List<Type>();
        PipelineConfiguration pipelineCollection;
        string publicReturnAddress;
        List<Action<IConfigureComponents>> registrations = new List<Action<IConfigureComponents>>();
        bool scanAssembliesInNestedDirectories;
        List<Type> scannedTypes;
        bool sendOnly;
    }
}