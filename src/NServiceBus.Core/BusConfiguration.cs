namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Transactions;
    using System.Web;
    using NServiceBus.Config.ConfigurationSource;
    using NServiceBus.Config.Conventions;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Container;
    using NServiceBus.Hosting.Helpers;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.ObjectBuilder.Autofac;
    using NServiceBus.ObjectBuilder.Common;
    using NServiceBus.Pipeline;
    using NServiceBus.Settings;
    using NServiceBus.Transports;
    using NServiceBus.Utils.Reflection;

    /// <summary>
    ///     Configuration used to create a bus instance.
    /// </summary>
    public partial class BusConfiguration : ExposeSettings
    {
        /// <summary>
        /// Initializes a fresh instance of the builder.
        /// </summary>
        public BusConfiguration()
            : base(new SettingsHolder())
        {
            configurationSourceToUse = new DefaultConfigurationSource();

            pipelineCollection = new PipelineConfiguration();
            Settings.Set<PipelineConfiguration>(pipelineCollection);
            Pipeline = new PipelineSettings(pipelineCollection.MainPipeline);

            Settings.Set<QueueBindings>(new QueueBindings());

            Settings.SetDefault("Endpoint.SendOnly", false);
            Settings.SetDefault("Transactions.Enabled", true);
            Settings.SetDefault("Transactions.IsolationLevel", IsolationLevel.ReadCommitted);
            Settings.SetDefault("Transactions.DefaultTimeout", TransactionManager.DefaultTimeout);
            Settings.SetDefault("Transactions.SuppressDistributedTransactions", false);
            Settings.SetDefault("Transactions.DoNotWrapHandlersExecutionInATransactionScope", false);
            Settings.SetDefault("DefaultSerializer", new XmlSerializer());

            conventionsBuilder = new ConventionsBuilder(Settings);
        }

        /// <summary>
        ///     Access to the pipeline configuration.
        /// </summary>
        public PipelineSettings Pipeline { get; private set; }

        /// <summary>
        ///     Used to configure components in the container.
        /// </summary>
        public void RegisterComponents(Action<IConfigureComponents> registration)
        {
            Guard.AgainstNull("registration", registration);
            registrations.Add(registration);
        }

        /// <summary>
        /// Append a list of <see cref="Assembly"/>s to the ignored list. The string is the file name of the assembly.
        /// </summary>
        public void ExcludeAssemblies(params string[] assemblies)
        {
            Guard.AgainstNull("assemblies", assemblies);

            if (assemblies.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException("Passed in a null or empty assembly name.", "assemblies");
            }
            excludedAssemblies = excludedAssemblies.Union(assemblies, StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        /// Append a list of <see cref="Type"/>s to the ignored list.
        /// </summary>
        public void ExcludeTypes(params Type[] types)
        {
            Guard.AgainstNull("types", types);
            if (types.Any(x => x == null))
            {
                throw new ArgumentException("Passed in a null or empty type.", "types");
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
        ///     Overrides the default configuration source.
        /// </summary>
        public void CustomConfigurationSource(IConfigurationSource configurationSource)
        {
            Guard.AgainstNull("configurationSource", configurationSource);
            configurationSourceToUse = configurationSource;
        }

        /// <summary>
        ///     Defines the name to use for this endpoint.
        /// </summary>
        public void EndpointName(string name)
        {
            Guard.AgainstNullAndEmpty("name", name);
            endpointName = name;
        }

        /// <summary>
        ///     Defines the conventions to use for this endpoint.
        /// </summary>
        public ConventionsBuilder Conventions()
        {
            return conventionsBuilder;
        }

        /// <summary>
        ///     Defines a custom builder to use.
        /// </summary>
        /// <typeparam name="T">The builder type of the <see cref="ContainerDefinition"/>.</typeparam>
        public void UseContainer<T>(Action<ContainerCustomizations> customizations = null) where T : ContainerDefinition, new()
        {
            if (customizations != null)
            {
                customizations(new ContainerCustomizations(Settings));
            }

            UseContainer(typeof(T));
        }

        /// <summary>
        ///     Defines a custom builder to use.
        /// </summary>
        /// <param name="definitionType">The type of the <see cref="ContainerDefinition"/>.</param>
        public void UseContainer(Type definitionType)
        {
            Guard.AgainstNull("definitionType", definitionType);
            Guard.TypeHasDefaultConstructor(definitionType, "definitionType");

            UseContainer(definitionType.Construct<ContainerDefinition>().CreateContainer(Settings));
        }

        /// <summary>
        ///     Uses an already active instance of a builder.
        /// </summary>
        /// <param name="builder">The instance to use.</param>
        public void UseContainer(IContainer builder)
        {
            Guard.AgainstNull("builder", builder);
            customBuilder = builder;
        }

        /// <summary>
        /// Sets the public return address of this endpoint.
        /// </summary>
        /// <param name="address">The public address.</param>
        public void OverridePublicReturnAddress(string address)
        {
            Guard.AgainstNullAndEmpty("address", address);
            Settings.SetDefault("PublicReturnAddress", address);
        }





        /// <summary>
        /// Sets the address of this endpoint.
        /// </summary>
        /// <param name="queue">The queue name.</param>
        public void OverrideLocalAddress(string queue)
        {
            Guard.AgainstNullAndEmpty("queue", queue);
            Settings.Set("NServiceBus.LocalAddress", queue);
        }

        /// <summary>
        ///     Specifies the range of types that NServiceBus scans for handlers etc.
        /// </summary>
        internal void TypesToScanInternal(IEnumerable<Type> typesToScan)
        {
            scannedTypes = typesToScan.ToList();
        }

        /// <summary>
        ///     Creates the configuration object.
        /// </summary>
        internal Configure BuildConfiguration()
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

            Configure.ActivateAndInvoke<INeedInitialization>(scannedTypes, t => t.Customize(this));

            UseTransportExtensions.SetupTransport(this);
            var container = customBuilder ?? new AutofacObjectBuilder();

            Settings.SetDefault<IConfigurationSource>(configurationSourceToUse);

            var endpointHelper = new EndpointHelper(new StackTrace());

            if (endpointVersion == null)
            {
                endpointVersion = endpointHelper.GetEndpointVersion();
            }

            if (endpointName == null)
            {
                endpointName = endpointHelper.GetDefaultEndpointName();
            }

            Settings.SetDefault("EndpointName", endpointName);
            Settings.SetDefault("EndpointVersion", endpointVersion);

            container.RegisterSingleton(typeof(Conventions), conventionsBuilder.Conventions);

            Settings.SetDefault<Conventions>(conventionsBuilder.Conventions);

            return new Configure(Settings, container, registrations, Pipeline, pipelineCollection);
        }

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
        List<Action<IConfigureComponents>> registrations = new List<Action<IConfigureComponents>>();
        IContainer customBuilder;
        string endpointName;
        string endpointVersion;
        IList<Type> scannedTypes;
        List<Type> excludedTypes = new List<Type>();
        List<string> excludedAssemblies = new List<string>();
        bool scanAssembliesInNestedDirectories;
        PipelineConfiguration pipelineCollection;
    }
}
