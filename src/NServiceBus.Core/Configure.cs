namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using Config;
    using Config.ConfigurationSource;
    using Config.Conventions;
    using Features;
    using Hosting.Helpers;
    using Logging;
    using ObjectBuilder;
    using ObjectBuilder.Common;
    using Pipeline;
    using Settings;
    using Utils.Reflection;

    /// <summary>
    ///     Central configuration entry point.
    /// </summary>
    public partial class Configure
    {
        /// <summary>
        ///     Protected constructor to enable creation only via the With method.
        /// </summary>
        internal Configure(SettingsHolder settings, IContainer container)
        {
            this.settings = settings;
            LogManager.HasConfigBeenInitialised = true;
            
            RegisterContainerAdapter(container);

            configurer.RegisterSingleton<Configure>(this);
            configurer.RegisterSingleton<ReadOnlySettings>(settings);
            
            settings.Set<PipelineModifications>(new PipelineModifications());
        }

        /// <summary>
        ///     Provides access to the settings holder
        /// </summary>
        public SettingsHolder Settings
        {
            get { return settings; }
        }


        /// <summary>
        ///     Gets the builder.
        /// </summary>
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
        }

        /// <summary>
        /// Access to the pipeline configuration
        /// </summary>
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

        void RegisterContainerAdapter(IContainer container)
        {
            if (builder != null)
            {
                throw new InvalidOperationException("Container adapter already specified");
            }

            var b = new CommonObjectBuilder { Container = container, Synchronized = settings.GetOrDefault<bool>("UseSyncronizationDomain") };

            builder = b;
            configurer = b;

            Configurer.ConfigureComponent<CommonObjectBuilder>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(c => c.Container, container);
        }


        void WireUpConfigSectionOverrides()
        {
            TypesToScan
                .Where(t => t.GetInterfaces().Any(IsGenericConfigSource))
                .ToList().ForEach(t => configurer.ConfigureComponent(t, DependencyLifecycle.InstancePerCall));
        }

        /// <summary>
        ///     Creates a new configuration object scanning assemblies in the regular runtime directory.
        /// </summary>
        public static Configure With()
        {
            return With(o => { });
        }


        /// <summary>
        /// Initializes the endpoint configuration with the specified customizations.
        /// </summary>
        /// <param name="customizations">The customizations builder.</param>
        /// <returns>A new endpoint configuration.</returns>
        public static Configure With(Action<ConfigurationBuilder> customizations)
        {
            var options = new ConfigurationBuilder();

            customizations(options);

            return With(options);
        }

        static Configure With(ConfigurationBuilder configurationBuilder)
        {
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

        /// <summary>
        ///     Finalizes the configuration by invoking all initialisers.
        /// </summary>
        internal void Initialize()
        {
            if (initialized)
            {
                return;
            }

            if (!HasBuilder())
            {
                this.DefaultBuilder();
            }

            Address.InitializeLocalAddress(settings.EndpointName());

            WireUpConfigSectionOverrides();

            featureActivator = new FeatureActivator(Settings);

            Configurer.RegisterSingleton<FeatureActivator>(featureActivator);

            ForAllTypes<Feature>(t => featureActivator.Add(t.Construct<Feature>()));

            ForAllTypes<IWantToRunWhenConfigurationIsComplete>(t => Configurer.ConfigureComponent(t, DependencyLifecycle.InstancePerCall));

            ForAllTypes<IWantToRunWhenBusStartsAndStops>(t => Configurer.ConfigureComponent(t, DependencyLifecycle.InstancePerCall));

            ActivateAndInvoke<INeedInitialization>(t => t.Init(this));

            ActivateAndInvoke<IWantToRunBeforeConfigurationIsFinalized>(t => t.Run(this));

            //lockdown the settings
            Settings.PreventChanges();

            featureActivator.SetupFeatures(new FeatureConfigurationContext(this));
            featureActivator.RegisterStartupTasks(Configurer);

            //this needs to be before the installers since they actually call .Initialize :(
            initialized = true;

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

        internal static IList<Type> GetAllowedTypes(params Assembly[] assemblies)
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

            // ReSharper disable HeapView.SlowDelegateCreation
            foreach (var detail in details.OrderByDescending(d => d.Item2))
            // ReSharper restore HeapView.SlowDelegateCreation
            {
                detailsMessage.AppendLine(string.Format("{0} - {1:f4} s", detail.Item1.FullName, detail.Item2.TotalSeconds));
            }


            if (logAsWarn)
            {
                logger.Warn(message + detailsMessage);
            }
            else
            {
                logger.Info(message);
                logger.Debug(detailsMessage.ToString());
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

        static ILog logger = LogManager.GetLogger<Configure>();
        bool initialized;
        IBuilder builder;
        IConfigureComponents configurer;
        FeatureActivator featureActivator;
        PipelineSettings pipelineSettings;
        SettingsHolder settings;

        /// <summary>
        /// Conventions builder class.
        /// </summary>
        public class ConventionsBuilder
        {
            /// <summary>
            ///     Sets the function to be used to evaluate whether a type is a message.
            /// </summary>
            public ConventionsBuilder DefiningMessagesAs(Func<Type, bool> definesMessageType)
            {
                this.definesMessageType = definesMessageType;
                return this;
            }

            /// <summary>
            ///     Sets the function to be used to evaluate whether a type is a commands.
            /// </summary>
            public ConventionsBuilder DefiningCommandsAs(Func<Type, bool> definesCommandType)
            {
                this.definesCommandType = definesCommandType;
                return this;
            }

            /// <summary>
            ///     Sets the function to be used to evaluate whether a type is a event.
            /// </summary>
            public ConventionsBuilder DefiningEventsAs(Func<Type, bool> definesEventType)
            {
                this.definesEventType = definesEventType;
                return this;
            }

            /// <summary>
            ///     Sets the function to be used to evaluate whether a property should be encrypted or not.
            /// </summary>
            public ConventionsBuilder DefiningEncryptedPropertiesAs(Func<PropertyInfo, bool> definesEncryptedProperty)
            {
                this.definesEncryptedProperty = definesEncryptedProperty;
                return this;
            }

            /// <summary>
            ///     Sets the function to be used to evaluate whether a property should be sent via the DataBus or not.
            /// </summary>
            public ConventionsBuilder DefiningDataBusPropertiesAs(Func<PropertyInfo, bool> definesDataBusProperty)
            {
                this.definesDataBusProperty = definesDataBusProperty;
                return this;
            }

            /// <summary>
            ///     Sets the function to be used to evaluate whether a message has a time to be received.
            /// </summary>
            public ConventionsBuilder DefiningTimeToBeReceivedAs(Func<Type, TimeSpan> retrieveTimeToBeReceived)
            {
                this.retrieveTimeToBeReceived = retrieveTimeToBeReceived;
                return this;
            }

            /// <summary>
            ///     Sets the function to be used to evaluate whether a type is an express message or not.
            /// </summary>
            public ConventionsBuilder DefiningExpressMessagesAs(Func<Type, bool> definesExpressMessageType)
            {
                this.definesExpressMessageType = definesExpressMessageType;
                return this;
            }

            internal Conventions BuildConventions()
            {
                var conventions = new Conventions(isCommandTypeAction: definesCommandType, isDataBusPropertyAction: definesDataBusProperty, isEncryptedPropertyAction: definesEncryptedProperty, isEventTypeAction: definesEventType, isExpressMessageAction: definesExpressMessageType, isMessageTypeAction: definesMessageType, timeToBeReceivedAction: retrieveTimeToBeReceived);
           
                return conventions;
            }

            Func<Type, bool> definesCommandType;
            Func<PropertyInfo, bool> definesDataBusProperty;
            Func<PropertyInfo, bool> definesEncryptedProperty;
            Func<Type, bool> definesEventType;
            Func<Type, bool> definesExpressMessageType;
            Func<Type, bool> definesMessageType;
            Func<Type, TimeSpan> retrieveTimeToBeReceived;
        }
    }
}
