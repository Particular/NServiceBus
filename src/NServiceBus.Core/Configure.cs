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
    using Installation.Environments;
    using Logging;
    using ObjectBuilder;
    using Settings;

    /// <summary>
    /// Central configuration entry point.
    /// </summary>
    public class Configure
    {
        static Configure()
        {
            ConfigurationSource = new DefaultConfigurationSource();
        }

        /// <summary>
        /// Provides static access to the configuration object.
        /// </summary>
        public static Configure Instance
        {
            get
            {
                //we can't check for null here since that would break the way we do extension methods (the must be on a instance)
                return instance;
            }
        }

        /// <summary>
        /// True if any of the <see cref="With()"/> has been called.
        /// </summary>
        public static bool WithHasBeenCalled()
        {
            return instance != null;
        }

        /// <summary>
        /// Event raised when configuration is complete.
        /// </summary>
        public static event Action ConfigurationComplete;

        /// <summary>
        /// Gets/sets the builder.
        /// </summary>
        /// <remarks>
        /// Setting the builder should only be done by NServiceBus framework code.
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
        /// True if a builder has been defined.
        /// </summary>
        public static bool BuilderIsConfigured()
        {
            if (!WithHasBeenCalled())
                return false;

            return Instance.HasBuilder();
        }

        bool HasBuilder()
        {
            return builder != null && configurer != null;
        }

        IBuilder builder;

        static bool initialized;

        /// <summary>
        /// Gets/sets the configuration source to be used.
        /// </summary>
        public static IConfigurationSource ConfigurationSource { get; set; }

        /// <summary>
        /// Sets the current configuration source.
        /// </summary>
        public Configure CustomConfigurationSource(IConfigurationSource configurationSource)
        {
            ConfigurationSource = configurationSource;
            return this;
        }

        /// <summary>
        /// Gets/sets the object used to configure components.
        /// This object should eventually reference the same container as the Builder.
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

        IConfigureComponents configurer;
        static bool configSectionOverridesInitialized;

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
        /// Protected constructor to enable creation only via the With method.
        /// </summary>
        protected Configure()
        {
        }

        static Endpoint endpoint;

        public static Endpoint Endpoint { get { return endpoint ?? (endpoint = new Endpoint()); } }

        static TransactionSettings transactionSetting;

        public static TransactionSettings Transactions { get { return transactionSetting ?? (transactionSetting = new TransactionSettings()); } }

        public static TransportSettings Transports { get { return transports ?? (transports = new TransportSettings()); } }

        static TransportSettings transports;

        public static FeatureSettings Features { get { return features ?? (features = new FeatureSettings()); } }

        static FeatureSettings features;

        public static SerializationSettings Serialization { get { return serialization ?? (serialization = new SerializationSettings()); } }

        static SerializationSettings serialization;

        /// <summary>
        /// Allows the user to control how the current endpoint behaves when scaled out.
        /// </summary>
        public static void ScaleOut(Action<ScaleOutSettings> customScaleOutSettings)
        {
            customScaleOutSettings(new ScaleOutSettings());
        }

        /// <summary>
        /// True if this endpoint is operating in send only mode.
        /// </summary>
        [ObsoleteEx(TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]
        public static bool SendOnlyMode { get { return SettingsHolder.Get<bool>("Endpoint.SendOnly"); } }

        /// <summary>
        /// Creates a new configuration object scanning assemblies in the regular runtime directory.
        /// </summary>
        public static Configure With()
        {
            if (HttpRuntime.AppDomainAppId != null)
                return With(HttpRuntime.BinDirectory);

            return With(AppDomain.CurrentDomain.BaseDirectory);
        }

        /// <summary>
        /// Configures NServiceBus to scan for assemblies in the relevant web directory instead of regular runtime directory.
        /// </summary>
        [ObsoleteEx(Replacement = "With()", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]
        public static Configure WithWeb()
        {
            return With();
        }

        /// <summary>
        /// Configure to scan for assemblies in the given directory rather than the regular runtime directory.
        /// </summary>
        public static Configure With(string probeDirectory)
        {
            lastProbeDirectory = probeDirectory;
            return With(GetAssembliesInDirectory(probeDirectory));
        }

        /// <summary>
        /// Configure to use the types found in the given assemblies.
        /// </summary>
        public static Configure With(IEnumerable<Assembly> assemblies)
        {
            return With(assemblies.ToArray());
        }

        /// <summary>
        /// Configure to scan the given assemblies only.
        /// </summary>
        public static Configure With(params Assembly[] assemblies)
        {
            var types = GetAllowedTypes(assemblies);

            return With(types);
        }

        /// <summary>
        /// Configure to scan the given types.
        /// </summary>
        public static Configure With(IEnumerable<Type> typesToScan)
        {
            if (instance == null)
            {
                instance = new Configure();
            }

            TypesToScan = typesToScan.Union(GetAllowedTypes(Assembly.GetExecutingAssembly())).ToList();

            if (HttpRuntime.AppDomainAppId == null)
            {
                var baseDirectory = lastProbeDirectory ?? AppDomain.CurrentDomain.BaseDirectory;
                var hostPath = Path.Combine(baseDirectory, "NServiceBus.Host.exe");
                if (File.Exists(hostPath))
                {
                    TypesToScan = TypesToScan.Union(GetAllowedTypes(Assembly.LoadFrom(hostPath))).ToList();
                }
            }

            //TODO: re-enable when we make message scanning lazy #1617
            //TypesToScan = TypesToScan.Union(GetMessageTypes(TypesToScan)).ToList();

            Logger.DebugFormat("Number of types to scan: {0}", TypesToScan.Count);

            EndpointHelper.StackTraceToExamine = new StackTrace();

            instance.InvokeISetDefaultSettings();

            return instance;
        }

        //TODO: re-enable when we make message scanning lazy #1617
        //static IEnumerable<Type> GetMessageTypes(IList<Type> types)
        //{
        //    return types.SelectMany(MessageHandlerRegistry.GetMessageTypesIfIsMessageHandler);
        //}

        /// <summary>
        /// Run a custom action at configuration time - useful for performing additional configuration not exposed by the fluent interface.
        /// </summary>
        public Configure RunCustomAction(Action action)
        {
            action();

            return this;
        }

        /// <summary>
        /// Provides an instance to a startable bus.
        /// </summary>
        public IStartableBus CreateBus()
        {
            Initialize();

            if (!Configurer.HasComponent<IStartableBus>())
            {
                Instance.UnicastBus();
            }

            return Builder.Build<IStartableBus>();
        }

        static bool beforeConfigurationInitializersCalled;
        bool invokeISetDefaultSettingsCalled;

        void InvokeISetDefaultSettings()
        {
            if (invokeISetDefaultSettingsCalled)
            {
                return;
            }

            ForAllTypes<ISetDefaultSettings>(t => Activator.CreateInstance(t));

            invokeISetDefaultSettingsCalled = true;
        }

        void InvokeBeforeConfigurationInitializers()
        {
            if (beforeConfigurationInitializersCalled)
            {
                return;
            }

            ActivateAndInvoke<IWantToRunBeforeConfiguration>(t => t.Init());

            beforeConfigurationInitializersCalled = true;
        }

        /// <summary>
        /// Finalizes the configuration by invoking all initialisers.
        /// </summary>
        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            ForAllTypes<IWantToRunWhenConfigurationIsComplete>(t => Configurer.ConfigureComponent(t, DependencyLifecycle.InstancePerCall));

            ForAllTypes<IWantToRunWhenBusStartsAndStops>(t => Configurer.ConfigureComponent(t, DependencyLifecycle.InstancePerCall));

            InvokeBeforeConfigurationInitializers();

            ActivateAndInvoke<INeedInitialization>(t => t.Init());

            ActivateAndInvoke<IWantToRunBeforeConfigurationIsFinalized>(t => t.Run());

            ForAllTypes<INeedToInstallSomething<Windows>>(t => Instance.Configurer.ConfigureComponent(t, DependencyLifecycle.InstancePerCall));

            //lockdown the settings
            SettingsHolder.PreventChanges();

            ActivateAndInvoke<IFinalizeConfiguration>(t => t.FinalizeConfiguration());

            initialized = true;

            if (ConfigurationComplete != null)
            {
                ConfigurationComplete();
            }

            Builder.BuildAll<IWantToRunWhenConfigurationIsComplete>()
                .ToList()
                .ForEach(o => o.Run());
        }


        /// <summary>
        /// Applies the given action to all the scanned types that can be assigned to <typeparamref name="T"/>.
        /// </summary>
        public void ForAllTypes<T>(Action<Type> action) where T : class
        {
            TypesToScan.Where(t => typeof(T).IsAssignableFrom(t) && !(t.IsAbstract || t.IsInterface))
              .ToList().ForEach(action);
        }

        /// <summary>
        /// Returns types in assemblies found in the current directory.
        /// </summary>
        public static IList<Type> TypesToScan { get; private set; }

        /// <summary>
        /// Returns the requested config section using the current configuration source.
        /// </summary>
        public static T GetConfigSection<T>() where T : class,new()
        {
            if (TypesToScan == null)
            {
                return ConfigurationSource.GetConfiguration<T>();
            }

            var sectionOverrideType = TypesToScan.FirstOrDefault(t => typeof (IProvideConfiguration<T>).IsAssignableFrom(t));

            if (sectionOverrideType == null)
            {
                return ConfigurationSource.GetConfiguration<T>();
            }

            var sectionOverride = (IProvideConfiguration<T>)Activator.CreateInstance(sectionOverrideType);

            return sectionOverride.GetConfiguration();
        }

        /// <summary>
        /// Load and return all assemblies in the given directory except the given ones to exclude.
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

        /// <summary>
        /// Configures the given type with the given <see cref="DependencyLifecycle"/>.
        /// </summary>
        public static IComponentConfig<T> Component<T>(DependencyLifecycle lifecycle)
        {
            if (Instance == null)
                throw new InvalidOperationException("You need to call Configure.With() before calling Configure.Component<T>()");

            return Instance.Configurer.ConfigureComponent<T>(lifecycle);
        }

        /// <summary>
        /// Configures the given type with the given lifecycle <see cref="DependencyLifecycle"/>.
        /// </summary>
        public static IComponentConfig Component(Type type, DependencyLifecycle lifecycle)
        {
            if (Instance == null)
                throw new InvalidOperationException("You need to call Configure.With() before calling Configure.Component()");

            return Instance.Configurer.ConfigureComponent(type, lifecycle);
        }

        /// <summary>
        /// Configures the given type with the given <see cref="DependencyLifecycle"/>.
        /// </summary>
        public static IComponentConfig<T> Component<T>(Func<T> componentFactory, DependencyLifecycle lifecycle)
        {
            if (Instance == null)
                throw new InvalidOperationException("You need to call Configure.With() before calling Configure.Component<T>()");

            return Instance.Configurer.ConfigureComponent(componentFactory, lifecycle);
        }

        /// <summary>
        /// Configures the given type with the given <see cref="DependencyLifecycle"/>
        /// </summary>
        public static IComponentConfig<T> Component<T>(Func<IBuilder,T> componentFactory, DependencyLifecycle lifecycle)
        {
	        if (Instance == null)
		        throw new InvalidOperationException("You need to call Configure.With() before calling Configure.Component<T>()");

	        return Instance.Configurer.ConfigureComponent(componentFactory, lifecycle);
        }
		  
        /// <summary>
        /// Returns true if a component of type <typeparamref name="T"/> exists in the container.
        /// </summary>
        public static bool HasComponent<T>()
        {
            return HasComponent(typeof(T));
        }


        /// <summary>
        /// Returns true if a component of type <paramref name="componentType"/> exists in the container.
        /// </summary>
        public static bool HasComponent(Type componentType)
        {
            if (Instance == null)
                throw new InvalidOperationException("You need to call Configure.With() before calling Configure.HasComponent");

            return Instance.Configurer.HasComponent(componentType);
        }


        /// <summary>
        /// The name of this endpoint.
        /// </summary>
        public static string EndpointName
        {
            get { return GetEndpointNameAction(); }
        }

        /// <summary>
        /// The function used to get the name of this endpoint.
        /// </summary>
        public static Func<string> GetEndpointNameAction = () => EndpointHelper.GetDefaultEndpointName();

        /// <summary>
        /// The function used to get the version of this endpoint.
        /// </summary>
        public static Func<string> DefineEndpointVersionRetriever = () => EndpointHelper.GetEndpointVersion();

        static IEnumerable<Type> GetAllowedTypes(params Assembly[] assemblies)
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
                        LogManager.GetLogger(typeof(Configure)).Warn(errorMessage);
                        //intentionally swallow exception
                    }
                });
            return types;
        }

        /// <summary>
        /// The function used to get the name of this endpoint.
        /// </summary>
        public static Func<FileInfo, Assembly> LoadAssembly = s => Assembly.LoadFrom(s.FullName);

        void ActivateAndInvoke<T>(Action<T> action, TimeSpan? thresholdForWarning = null) where T : class
        {
            if (!thresholdForWarning.HasValue)
                thresholdForWarning = TimeSpan.FromSeconds(5);

            var totalTime = new Stopwatch();

            totalTime.Start();

            var details = new List<Tuple<Type, TimeSpan>>();

            ForAllTypes<T>(t =>
            {
                var sw = new Stopwatch();

                sw.Start();
                var instanceToInvoke = (T)Activator.CreateInstance(t);
                action(instanceToInvoke);
                sw.Stop();

                details.Add(new Tuple<Type, TimeSpan>(t, sw.Elapsed));
            });

            totalTime.Stop();

            var message = string.Format("Invocation of {0} completed in {1:f2} s", typeof(T).FullName, totalTime.Elapsed.TotalSeconds);

            var logAsWarn = details.Any(d => d.Item2 > thresholdForWarning);

            var detailsMessage = new StringBuilder();

            detailsMessage.AppendLine(" - Details:");

            foreach (var detail in details.OrderByDescending(d => d.Item2))
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

        static string lastProbeDirectory;
        static Configure instance;
        static ILog Logger
        {
            get
            {
                return LogManager.GetLogger(typeof(Configure));
            }
        }
    }
}
