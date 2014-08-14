namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Transactions;
    using System.Web;
    using NServiceBus.Config.ConfigurationSource;
    using NServiceBus.Config.Conventions;
    using NServiceBus.Container;
    using NServiceBus.Hosting.Helpers;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.ObjectBuilder.Autofac;
    using NServiceBus.ObjectBuilder.Common;
    using NServiceBus.Pipeline;
    using NServiceBus.Settings;
    using NServiceBus.Utils.Reflection;

    /// <summary>
    ///     Builder that construct the endpoint configuration.
    /// </summary>
    public class ConfigurationBuilder
    {
        internal ConfigurationBuilder()
        {
            configurationSourceToUse = new DefaultConfigurationSource();
            settings.Set<PipelineModifications>(new PipelineModifications());
            Pipeline = new PipelineSettings(this);
        }

        /// <summary>
        ///     Access to the pipeline configuration
        /// </summary>
        public PipelineSettings Pipeline { get; private set; }

        /// <summary>
        ///     Used to configure components in the container.
        /// </summary>
        public void RegisterComponents(Action<IConfigureComponents> registration)
        {
            registrations.Add(registration);
        }

        /// <summary>
        ///     Specifies the range of types that NServiceBus scans for handlers etc.
        /// </summary>
        public void TypesToScan(IEnumerable<Type> typesToScan)
        {
            scannedTypes = typesToScan.ToList();
        }

        /// <summary>
        ///     The assemblies to include when scanning for types.
        /// </summary>
        public void AssembliesToScan(IEnumerable<Assembly> assemblies)
        {
            AssembliesToScan(assemblies.ToArray());
        }

        /// <summary>
        ///     The assemblies to include when scanning for types.
        /// </summary>
        public void AssembliesToScan(params Assembly[] assemblies)
        {
            scannedTypes = Configure.GetAllowedTypes(assemblies);
        }


        /// <summary>
        ///     Specifies the directory where NServiceBus scans for types.
        /// </summary>
        public void ScanAssembliesInDirectory(string probeDirectory)
        {
            directory = probeDirectory;
            AssembliesToScan(GetAssembliesInDirectory(probeDirectory));
        }

        /// <summary>
        ///     Overrides the default configuration source.
        /// </summary>
        public void CustomConfigurationSource(IConfigurationSource configurationSource)
        {
            configurationSourceToUse = configurationSource;
        }


        /// <summary>
        ///     Defines the name to use for this endpoint.
        /// </summary>
        public void EndpointName(string name)
        {
            endpointName = name;
        }

        /// <summary>
        ///     Defines the version of this endpoint.
        /// </summary>
        public void EndpointVersion(string version)
        {
            endpointVersion = version;
        }

        /// <summary>
        ///     Defines the conventions to use for this endpoint.
        /// </summary>
        public void Conventions(Action<ConventionsBuilder> conventions)
        {
            conventions(conventionsBuilder);
        }

        /// <summary>
        ///     Defines a custom builder to use
        /// </summary>
        /// <typeparam name="T">The builder type</typeparam>
        public void UseContainer<T>(Action<ContainerCustomizations> customizations = null) where T : ContainerDefinition, new()
        {
            if (customizations != null)
            {
                customizations(new ContainerCustomizations(settings));
            }

            UseContainer(typeof(T));
        }

        /// <summary>
        ///     Defines a custom builder to use
        /// </summary>
        /// <param name="definitionType">The type of the builder</param>
        public void UseContainer(Type definitionType)
        {
            UseContainer(definitionType.Construct<ContainerDefinition>().CreateContainer(settings));
        }

        /// <summary>
        ///     Uses an already active instance of a builder
        /// </summary>
        /// <param name="builder">The instance to use</param>
        public void UseContainer(IContainer builder)
        {
            customBuilder = builder;
        }

        /// <summary>
        ///     Creates the configuration object
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

                ScanAssembliesInDirectory(directoryToScan);
            }

            scannedTypes = scannedTypes.Union(Configure.GetAllowedTypes(Assembly.GetExecutingAssembly())).ToList();

            if (HttpRuntime.AppDomainAppId == null)
            {
                var baseDirectory = directory ?? AppDomain.CurrentDomain.BaseDirectory;
                var hostPath = Path.Combine(baseDirectory, "NServiceBus.Host.exe");
                if (File.Exists(hostPath))
                {
                    scannedTypes = scannedTypes.Union(Configure.GetAllowedTypes(Assembly.LoadFrom(hostPath))).ToList();
                }
            }

            UseTransportExtensions.SetupTransport(this);
            var container = customBuilder ?? new AutofacObjectBuilder();
            RegisterEndpointWideDefaults();

            var conventions = conventionsBuilder.BuildConventions();
            container.RegisterSingleton(typeof(Conventions), conventions);

            settings.SetDefault<Conventions>(conventions);

            Configure.ActivateAndInvoke<INeedInitialization>(scannedTypes, t => t.Customize(this));

            return new Configure(settings, container, registrations, Pipeline);
        }

        IEnumerable<Assembly> GetAssembliesInDirectory(string path, params string[] assembliesToSkip)
        {
            var assemblyScanner = new AssemblyScanner(path);
            assemblyScanner.MustReferenceAtLeastOneAssembly.Add(typeof(IHandleMessages<>).Assembly);
            if (assembliesToSkip != null)
            {
                assemblyScanner.AssembliesToSkip = assembliesToSkip.ToList();
            }
            return assemblyScanner
                .GetScannableAssemblies()
                .Assemblies;
        }

        void RegisterEndpointWideDefaults()
        {
            var endpointHelper = new EndpointHelper(new StackTrace());

            if (endpointVersion == null)
            {
                endpointVersion = endpointHelper.GetEndpointVersion();
            }

            if (endpointName == null)
            {
                endpointName = endpointHelper.GetDefaultEndpointName();
            }

            settings.SetDefault("EndpointName", endpointName);
            settings.SetDefault("TypesToScan", scannedTypes);
            settings.SetDefault("EndpointVersion", endpointVersion);
            settings.SetDefault("Endpoint.SendOnly", false);
            settings.SetDefault("Transactions.Enabled", true);
            settings.SetDefault("Transactions.IsolationLevel", IsolationLevel.ReadCommitted);
            settings.SetDefault("Transactions.DefaultTimeout", TransactionManager.DefaultTimeout);
            settings.SetDefault("Transactions.SuppressDistributedTransactions", false);
            settings.SetDefault("Transactions.DoNotWrapHandlersExecutionInATransactionScope", false);
            settings.SetDefault<IConfigurationSource>(configurationSourceToUse);
        }

        IConfigurationSource configurationSourceToUse;
        ConventionsBuilder conventionsBuilder = new ConventionsBuilder();
        List<Action<IConfigureComponents>> registrations = new List<Action<IConfigureComponents>>();
        IContainer customBuilder;
        string directory;
        string endpointName;
        string endpointVersion;
        IList<Type> scannedTypes;
        internal SettingsHolder settings = new SettingsHolder();
    }
}
