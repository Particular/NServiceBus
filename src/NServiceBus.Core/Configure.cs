namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Web;
    using Config;
    using Config.ConfigurationSource;
    using Config.Conventions;
    using Features;
    using Hosting.Helpers;
    using Installation;
    using Logging;
    using ObjectBuilder;
    using Pipeline;
    using Settings;

    /// <summary>
    ///     Central configuration entry point.
    /// </summary>
    public partial class Configure
    {
        /// <summary>
        ///     Protected constructor to enable creation only via the With method.
        /// </summary>
        Configure(string endpointName, IList<Type> availableTypes, IConfigurationSource configurationSource)
        {
            LogManager.HasConfigBeenInitialised = true;
            Settings.SetDefault("EndpointName", endpointName);
            Settings.SetDefault("TypesToScan", availableTypes);
            Settings.SetDefault<IConfigurationSource>(configurationSource);
        }

        /// <summary>
        ///     Provides access to the settings holder
        /// </summary>
        public SettingsHolder Settings
        {
            get { return SettingsHolder.Instance; }
        }


        /// <summary>
        ///     Gets/sets the builder.
        /// </summary>
        /// <remarks>
        ///     Setting the builder should only be done by NServiceBus framework code.
        /// </remarks>
        public IBuilder Builder
        {
            get
            {
                if (builder == null)
                {
                    throw new InvalidOperationException("You can't access Configure.Instance.Builder before calling specifying a builder. Please add a call to Configure.DefaultBuilder() or any of the other supported builders to set one up");
                }

                return builder;
            }
            set { builder = value; }
        }

        /// <summary>
        ///     Gets/sets the object used to configure components.
        ///     This object should eventually reference the same container as the Builder.
        /// </summary>
        public IConfigureComponents Configurer
        {
            get
            {
                if (configurer == null)
                {
                    throw new InvalidOperationException("You can't access Configure.Instance.Configurer before calling specifying a builder. Please add a call to Configure.DefaultBuilder() or any of the other supported builders to set one up");
                }

                return configurer;
            }
            set
            {
                configurer = value;
                WireUpConfigSectionOverrides();
                InvokeBeforeConfigurationInitializers();
            }
        }

        public SerializationSettings Serialization
        {
            get { return serialization ?? (serialization = new SerializationSettings(this)); }
        }

        public PipelineSettings Pipeline
        {
            get { return pipelineSettings ?? (pipelineSettings = new PipelineSettings(this)); }
        }

        /// <summary>
        ///     Returns types in assemblies found in the current directory.
        /// </summary>
        public IList<Type> TypesToScan
        {
            get { return Settings.GetAvailableTypes(); }
        }

        bool HasBuilder()
        {
            return builder != null && configurer != null;
        }


        void WireUpConfigSectionOverrides()
        {
            if (configSectionOverridesInitialized)
            {
                return;
            }

            TypesToScan
                .Where(t => t.GetInterfaces().Any(IsGenericConfigSource))
                .ToList().ForEach(t => configurer.ConfigureComponent(t, DependencyLifecycle.InstancePerCall));

            configSectionOverridesInitialized = true;
        }

        /// <summary>
        ///     Creates a new configuration object scanning assemblies in the regular runtime directory.
        /// </summary>
        public static Configure With()
        {
            return With(o => { });
        }


        public static Configure With(Action<ConfigurationBuilder> customizations)
        {
            var options = new ConfigurationBuilder();

            customizations(options);

            return With(options);
        }

        static Configure With(ConfigurationBuilder configurationBuilder)
        {
            SettingsHolder.Instance.Reset();
            instance = configurationBuilder.BuildConfiguration();

            EndpointHelper.StackTraceToExamine = new StackTrace();

            return instance;
        }

        /// <summary>
        ///     Provides an instance to a startable bus.
        /// </summary>
        public IStartableBus CreateBus()
        {
            Initialize();

            return Builder.Build<IStartableBus>();
        }

        void InvokeBeforeConfigurationInitializers()
        {
            if (beforeConfigurationInitializersCalled)
            {
                return;
            }

            ActivateAndInvoke<IWantToRunBeforeConfiguration>(t => t.Init(this));

            beforeConfigurationInitializersCalled = true;
        }

        /// <summary>
        ///     Finalizes the configuration by invoking all initialisers.
        /// </summary>
        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            if (!HasBuilder())
            {
                this.DefaultBuilder();
            }

            featureActivator = new FeatureActivator(Settings);

            Configurer.RegisterSingleton<FeatureActivator>(featureActivator);

            ForAllTypes<Feature>(t => featureActivator.Add((Feature) Activator.CreateInstance(t)));

            ForAllTypes<IWantToRunWhenConfigurationIsComplete>(t => Configurer.ConfigureComponent(t, DependencyLifecycle.InstancePerCall));

            ForAllTypes<IWantToRunWhenBusStartsAndStops>(t => Configurer.ConfigureComponent(t, DependencyLifecycle.InstancePerCall));

            InvokeBeforeConfigurationInitializers();

            ActivateAndInvoke<INeedInitialization>(t => t.Init(this));


            ActivateAndInvoke<IWantToRunBeforeConfigurationIsFinalized>(t => t.Run(this));

            //lockdown the settings
            Settings.PreventChanges();

            ActivateAndInvoke<IFinalizeConfiguration>(t => t.FinalizeConfiguration(this));

            featureActivator.SetupFeatures(new FeatureConfigurationContext(this));
            featureActivator.RegisterStartupTasks(Configurer);

            //this needs to be before the installers since they actually call .Initialize :(
            initialized = true;

            ForAllTypes<INeedToInstallSomething>(t => Instance.Configurer.ConfigureComponent(t, DependencyLifecycle.InstancePerCall));


            Builder.BuildAll<IWantToRunWhenConfigurationIsComplete>()
                .ToList()
                .ForEach(o => o.Run(this));
        }


        /// <summary>
        ///     Applies the given action to all the scanned types that can be assigned to <typeparamref name="T" />.
        /// </summary>
        public void ForAllTypes<T>(Action<Type> action) where T : class
        {
            // ReSharper disable HeapView.SlowDelegateCreation
            TypesToScan.Where(t => typeof(T).IsAssignableFrom(t) && !(t.IsAbstract || t.IsInterface))
                // ReSharper restore HeapView.SlowDelegateCreation
                .ToList().ForEach(action);
        }


        /// <summary>
        ///     Load and return all assemblies in the given directory except the given ones to exclude.
        /// </summary>
        /// <param name="path">Path to scan</param>
        /// <param name="assembliesToSkip">The exclude must either be the full assembly name or a prefix-pattern.</param>
        public static IEnumerable<Assembly> GetAssembliesInDirectory(string path, params string[] assembliesToSkip)
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

        static IList<Type> GetAllowedTypes(params Assembly[] assemblies)
        {
            var types = new List<Type>();
            Array.ForEach(
                assemblies,
                a =>
                {
                    try
                    {
                        types.AddRange(a.GetTypes()
                            .Where(AssemblyScanner.IsAllowedType));
                    }
                    catch (ReflectionTypeLoadException e)
                    {
                        var errorMessage = AssemblyScanner.FormatReflectionTypeLoadException(a.FullName, e);
                        LogManager.GetLogger<Configure>().Warn(errorMessage);
                        //intentionally swallow exception
                    }
                });
            return types;
        }

        void ActivateAndInvoke<T>(Action<T> action, TimeSpan? thresholdForWarning = null) where T : class
        {
            if (!thresholdForWarning.HasValue)
            {
                thresholdForWarning = TimeSpan.FromSeconds(5);
            }

            var totalTime = new Stopwatch();

            totalTime.Start();

            var details = new List<Tuple<Type, TimeSpan>>();

            ForAllTypes<T>(t =>
            {
                var sw = new Stopwatch();

                sw.Start();
                var instanceToInvoke = (T) Activator.CreateInstance(t);
                action(instanceToInvoke);
                sw.Stop();

                details.Add(new Tuple<Type, TimeSpan>(t, sw.Elapsed));
            });

            totalTime.Stop();

            var message = string.Format("Invocation of {0} completed in {1:f2} s", typeof(T).FullName, totalTime.Elapsed.TotalSeconds);

            var logAsWarn = details.Any(d => d.Item2 > thresholdForWarning);

            var detailsMessage = new StringBuilder();

            detailsMessage.AppendLine(" - Details:");

            // ReSharper disable HeapView.SlowDelegateCreation
            foreach (var detail in details.OrderByDescending(d => d.Item2))
                // ReSharper restore HeapView.SlowDelegateCreation
            {
                detailsMessage.AppendLine(string.Format("{0} - {1:f4} s", detail.Item1.FullName, detail.Item2.TotalSeconds));
            }


            if (logAsWarn)
            {
                Logger.Warn(message + detailsMessage);
            }
            else
            {
                Logger.Info(message);
                Logger.Debug(detailsMessage.ToString());
            }
        }

        static bool IsGenericConfigSource(Type t)
        {
            if (!t.IsGenericType)
            {
                return false;
            }

            var args = t.GetGenericArguments();
            if (args.Length != 1)
            {
                return false;
            }

            return typeof(IProvideConfiguration<>).MakeGenericType(args).IsAssignableFrom(t);
        }


        static bool configSectionOverridesInitialized;
        static bool beforeConfigurationInitializersCalled;


        public static Func<FileInfo, Assembly> LoadAssembly = s => Assembly.LoadFrom(s.FullName);

        static bool initialized;
        IBuilder builder;
        IConfigureComponents configurer;
        FeatureActivator featureActivator;
        PipelineSettings pipelineSettings;
        SerializationSettings serialization;


        public class ConfigurationBuilder
        {
            internal ConfigurationBuilder()
            {
                configurationSourceToUse = new DefaultConfigurationSource();
            }

            /// <summary>
            ///     Specifies the range of types that NServiceBus scans for handlers etc
            /// </summary>
            /// <param name="typesToScan"></param>
            public void TypesToScan(IEnumerable<Type> typesToScan)
            {
                scannedTypes = typesToScan.ToList();
            }

            /// <summary>
            ///     The assemblies to include when scanning for types
            /// </summary>
            /// <param name="assemblies"></param>
            public void AssembliesToScan(IEnumerable<Assembly> assemblies)
            {
                AssembliesToScan(assemblies.ToArray());
            }

            /// <summary>
            ///     The assemblies to include when scanning for types
            /// </summary>
            /// <param name="assemblies"></param>
            public void AssembliesToScan(params Assembly[] assemblies)
            {
                scannedTypes = GetAllowedTypes(assemblies);
            }


            /// <summary>
            ///     Specifies the directory where NServiceBus scans for types
            /// </summary>
            /// <param name="probeDirectory"></param>
            public void ScanAssembliesInDirectory(string probeDirectory)
            {
                directory = probeDirectory;
                AssembliesToScan(GetAssembliesInDirectory(probeDirectory));
            }


            /// <summary>
            ///     Overrides the default configuration source
            /// </summary>
            /// <param name="configurationSource"></param>
            public void CustomConfigurationSource(IConfigurationSource configurationSource)
            {
                configurationSourceToUse = configurationSource;
            }


            /// <summary>
            ///     Defines the name to use for this endpoint
            /// </summary>
            /// <param name="name"></param>
            public void EndpointName(string name)
            {
                EndpointName(() => name);
            }

            /// <summary>
            ///     Defines the name to use for this endpoint
            /// </summary>
            /// <param name="nameFunc"></param>
            public void EndpointName(Func<string> nameFunc)
            {
                getEndpointNameAction = nameFunc;
            }

            /// <summary>
            ///     Creates the configuration object
            /// </summary>
            /// <returns></returns>
            internal Configure BuildConfiguration()
            {
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

                scannedTypes = scannedTypes.Union(GetAllowedTypes(Assembly.GetExecutingAssembly())).ToList();

                if (HttpRuntime.AppDomainAppId == null)
                {
                    var baseDirectory = directory ?? AppDomain.CurrentDomain.BaseDirectory;
                    var hostPath = Path.Combine(baseDirectory, "NServiceBus.Host.exe");
                    if (File.Exists(hostPath))
                    {
                        scannedTypes = scannedTypes.Union(GetAllowedTypes(Assembly.LoadFrom(hostPath))).ToList();
                    }
                }
                return new Configure(endpointName, scannedTypes, configurationSourceToUse);
            }


            IConfigurationSource configurationSourceToUse;
            string directory;
            string endpointName;
            Func<string> getEndpointNameAction = () => EndpointHelper.GetDefaultEndpointName();
            IList<Type> scannedTypes;
        }


        // ReSharper restore UnusedParameter.Global
    }
}