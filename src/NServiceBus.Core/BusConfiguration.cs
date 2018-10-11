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
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Container;
    using NServiceBus.Hosting.Helpers;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.ObjectBuilder.Autofac;
    using NServiceBus.ObjectBuilder.Common;
    using NServiceBus.Pipeline;
    using NServiceBus.Settings;
    using NServiceBus.Utils;
    using NServiceBus.Utils.Reflection;

    /// <summary>
    ///     Configuration used to create a bus instance
    /// </summary>
    public class BusConfiguration : ExposeSettings
    {
        /// <summary>
        /// Initializes a fresh instance of the builder
        /// </summary>
        public BusConfiguration()
            : base(new SettingsHolder())
        {
            configurationSourceToUse = new DefaultConfigurationSource();
            Settings.Set<PipelineModifications>(new PipelineModifications());
            Pipeline = new PipelineSettings(this);

            Settings.SetDefault("Endpoint.SendOnly", false);
            Settings.SetDefault("Transactions.Enabled", true);
            Settings.SetDefault("Transactions.IsolationLevel", IsolationLevel.ReadCommitted);
            Settings.SetDefault("Transactions.DefaultTimeout", TransactionManager.DefaultTimeout);
            Settings.SetDefault("Transactions.SuppressDistributedTransactions", false);
            Settings.SetDefault("Transactions.DoNotWrapHandlersExecutionInATransactionScope", false);
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
        [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5.2", Message = "This api does not do anything.")]
        public void EndpointVersion(string version)
        {
            endpointVersion = version;
        }

        /// <summary>
        ///     Defines the conventions to use for this endpoint.
        /// </summary>
        public ConventionsBuilder Conventions()
        {
            return conventionsBuilder;
        }

        /// <summary>
        ///     Defines a custom builder to use
        /// </summary>
        /// <typeparam name="T">The builder type</typeparam>
        public void UseContainer<T>(Action<ContainerCustomizations> customizations = null) where T : ContainerDefinition, new()
        {
            if (customizations != null)
            {
                customizations(new ContainerCustomizations(Settings));
            }

            UseContainer(typeof(T));
        }

        /// <summary>
        ///     Defines a custom builder to use
        /// </summary>
        /// <param name="definitionType">The type of the builder</param>
        public void UseContainer(Type definitionType)
        {
            Guard.TypeHasDefaultConstructor(definitionType, "definitionType");

            UseContainer(definitionType.Construct<ContainerDefinition>().CreateContainer(Settings));
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
        /// Sets the public return address of this endpoint.
        /// </summary>
        /// <param name="address">The public address.</param>
        public void OverridePublicReturnAddress(Address address)
        {
            publicReturnAddress = address;
        }

        /// <summary>
        /// Sets the address of this endpoint.
        /// </summary>
        /// <param name="queue">The queue name.</param>
        public void OverrideLocalAddress(string queue)
        {
            Settings.Set("NServiceBus.LocalAddress", queue);
        }

        /// <summary>
        ///     Creates the configuration object
        /// </summary>
        internal Configure BuildConfiguration()
        {
            if (scannedTypes == null)
            {
                var directoryToScan = AppDomain.CurrentDomain.BaseDirectory;
                // FalsePositive, see https://youtrack.jetbrains.com/issue/RSRP-465918
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (HttpRuntime.AppDomainAppId != null)
                {
                    directoryToScan = HttpRuntime.BinDirectory;
                }

                ScanAssembliesInDirectory(directoryToScan);
            }

            scannedTypes = scannedTypes.Union(Configure.GetAllowedTypes(Assembly.GetExecutingAssembly())).ToList();

            // FalsePositive, see https://youtrack.jetbrains.com/issue/RSRP-465918
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            // ReSharper disable HeuristicUnreachableCode
            if (HttpRuntime.AppDomainAppId == null)
            {
                var baseDirectory = directory ?? AppDomain.CurrentDomain.BaseDirectory;
                var hostPath = Path.Combine(baseDirectory, "NServiceBus.Host.exe");
                if (File.Exists(hostPath))
                {
                    scannedTypes = scannedTypes.Union(Configure.GetAllowedTypes(Assembly.LoadFrom(hostPath))).ToList();
                }
            }
            // ReSharper restore HeuristicUnreachableCode

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

            if (publicReturnAddress != null)
            {
                Settings.SetDefault("PublicReturnAddress", publicReturnAddress);
            }

            container.RegisterSingleton(typeof(Conventions), conventionsBuilder.Conventions);

            Settings.SetDefault<Conventions>(conventionsBuilder.Conventions);

            return new Configure(Settings, container, registrations, Pipeline);
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

        IConfigurationSource configurationSourceToUse;
        ConventionsBuilder conventionsBuilder = new ConventionsBuilder();
        List<Action<IConfigureComponents>> registrations = new List<Action<IConfigureComponents>>();
        IContainer customBuilder;
        string directory;
        string endpointName;
        string endpointVersion;
        IList<Type> scannedTypes;
        Address publicReturnAddress;
    }
}