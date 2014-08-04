namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Web;
    using Config.ConfigurationSource;
    using Config.Conventions;
    using Container;
    using ObjectBuilder.Autofac;
    using ObjectBuilder.Common;
    using Settings;
    using Utils.Reflection;

    /// <summary>
    ///     Builder that construct the endpoint configuration.
    /// </summary>
    public class ConfigurationBuilder
    {
        internal ConfigurationBuilder()
        {
            configurationSourceToUse = new DefaultConfigurationSource();
        }

        /// <summary>
        ///     Specifies the range of types that NServiceBus scans for handlers etc.
        /// </summary>
        public ConfigurationBuilder TypesToScan(IEnumerable<Type> typesToScan)
        {
            scannedTypes = typesToScan.ToList();
            return this;
        }

        /// <summary>
        ///     The assemblies to include when scanning for types.
        /// </summary>
        public ConfigurationBuilder AssembliesToScan(IEnumerable<Assembly> assemblies)
        {
            AssembliesToScan(assemblies.ToArray());
            return this;
        }

        /// <summary>
        ///     The assemblies to include when scanning for types.
        /// </summary>
        public ConfigurationBuilder AssembliesToScan(params Assembly[] assemblies)
        {
            scannedTypes = Configure.GetAllowedTypes(assemblies);
            return this;
        }


        /// <summary>
        ///     Specifies the directory where NServiceBus scans for types.
        /// </summary>
        public ConfigurationBuilder ScanAssembliesInDirectory(string probeDirectory)
        {
            directory = probeDirectory;
            AssembliesToScan(Configure.GetAssembliesInDirectory(probeDirectory));
            return this;
        }


        /// <summary>
        ///     Overrides the default configuration source.
        /// </summary>
        public ConfigurationBuilder CustomConfigurationSource(IConfigurationSource configurationSource)
        {
            configurationSourceToUse = configurationSource;
            return this;
        }


        /// <summary>
        ///     Defines the name to use for this endpoint.
        /// </summary>
        public ConfigurationBuilder EndpointName(string name)
        {
            EndpointName(() => name);
            return this;
        }

        /// <summary>
        ///     Defines the name to use for this endpoint.
        /// </summary>
        public ConfigurationBuilder EndpointName(Func<string> nameFunc)
        {
            getEndpointNameAction = nameFunc;
            return this;
        }

        /// <summary>
        ///     Defines the version of this endpoint.
        /// </summary>
        public ConfigurationBuilder EndpointVersion(string version)
        {
            EndpointVersion(() => version);
            return this;
        }

        /// <summary>
        ///     Defines the version of this endpoint.
        /// </summary>
        public ConfigurationBuilder EndpointVersion(Func<string> versionFunc)
        {
            getEndpointVersionAction = versionFunc;
            return this;
        }

        /// <summary>
        ///     Defines the conventions to use for this endpoint.
        /// </summary>
        public ConfigurationBuilder Conventions(Action<Configure.ConventionsBuilder> conventions)
        {
            conventions(conventionsBuilder);

            return this;
        }

        /// <summary>
        ///     Defines a custom builder to use
        /// </summary>
        /// <typeparam name="T">The builder type</typeparam>
        public ConfigurationBuilder UseContainer<T>(Action<ContainerCustomizations> customizations = null) where T : ContainerDefinition, new()
        {
            if (customizations != null)
            {
                customizations(new ContainerCustomizations(settings));
            }

            return UseContainer(typeof(T));
        }

        /// <summary>
        ///     Defines a custom builder to use
        /// </summary>
        /// <param name="definitionType">The type of the builder</param>
        public ConfigurationBuilder UseContainer(Type definitionType)
        {
            return UseContainer(definitionType.Construct<ContainerDefinition>().CreateContainer(settings));
        }

        /// <summary>
        ///     Uses an already active instance of a builder
        /// </summary>
        /// <param name="builder">The instance to use</param>
        public ConfigurationBuilder UseContainer(IContainer builder)
        {
            customBuilder = builder;

            return this;
        }

        /// <summary>
        ///     Creates the configuration object
        /// </summary>
        internal Configure BuildConfiguration()
        {
            var version = getEndpointVersionAction();

            endpointName = getEndpointNameAction();

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
            var container = customBuilder ?? new AutofacObjectBuilder();
            settings.SetDefault("EndpointName", endpointName);
            settings.SetDefault("TypesToScan", scannedTypes);
            settings.SetDefault("EndpointVersion", version);

            var conventions = conventionsBuilder.BuildConventions();
            container.RegisterSingleton(typeof(Conventions), conventions);

            settings.SetDefault<IConfigurationSource>(configurationSourceToUse);
            settings.SetDefault<Conventions>(conventions);

            return new Configure(settings, container);
        }

        IConfigurationSource configurationSourceToUse;
        Configure.ConventionsBuilder conventionsBuilder = new Configure.ConventionsBuilder();
        IContainer customBuilder;
        string directory;
        string endpointName;
        Func<string> getEndpointNameAction = () => EndpointHelper.GetDefaultEndpointName();
        Func<string> getEndpointVersionAction = () => EndpointHelper.GetEndpointVersion();
        IList<Type> scannedTypes;
        internal SettingsHolder settings = new SettingsHolder();
    }

}
