namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using NServiceBus.Config;
    using NServiceBus.Config.ConfigurationSource;
    using NServiceBus.Features;
    using NServiceBus.Hosting.Helpers;
    using NServiceBus.Logging;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.ObjectBuilder.Common;
    using NServiceBus.Pipeline;
    using NServiceBus.Settings;
    using NServiceBus.Utils.Reflection;

    /// <summary>
    ///     Central configuration entry point.
    /// </summary>
    public partial class Configure
    {
        /// <summary>
        ///     Protected constructor to enable creation only via the With method.
        /// </summary>
        internal Configure(SettingsHolder settings, IContainer container, List<Action<IConfigureComponents>> registrations)
        {
            Settings = settings;
            LogManager.HasConfigBeenInitialised = true;

            RegisterContainerAdapter(container);
            RunUserRegistrations(registrations);

            Configurer.RegisterSingleton(this);
            Configurer.RegisterSingleton<ReadOnlySettings>(settings);

            settings.Set<PipelineModifications>(new PipelineModifications());
            Pipeline = new PipelineSettings(this);
        }

        /// <summary>
        ///     Provides access to the settings holder
        /// </summary>
        public SettingsHolder Settings { get; private set; }

        /// <summary>
        ///     Gets the builder.
        /// </summary>
        public IBuilder Builder { get; private set; }

        /// <summary>
        ///     Gets/sets the object used to configure components.
        ///     This object should eventually reference the same container as the Builder.
        /// </summary>
        public IConfigureComponents Configurer { get; private set; }

        /// <summary>
        ///     Access to the pipeline configuration
        /// </summary>
        public PipelineSettings Pipeline { get; private set; }

        /// <summary>
        ///     Returns types in assemblies found in the current directory.
        /// </summary>
        public IList<Type> TypesToScan
        {
            get { return Settings.GetAvailableTypes(); }
        }

        void RunUserRegistrations(List<Action<IConfigureComponents>> registrations)
        {
            foreach (var registration in registrations)
            {
                registration(Configurer);
            }
        }

        void RegisterContainerAdapter(IContainer container)
        {
            var b = new CommonObjectBuilder
            {
                Container = container,
                Synchronized = Settings.GetOrDefault<bool>("UseSynchronizationDomain")
            };

            Builder = b;
            Configurer = b;

            Configurer.ConfigureComponent<CommonObjectBuilder>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(c => c.Container, container);
        }


        void WireUpConfigSectionOverrides()
        {
            TypesToScan
                .Where(t => t.GetInterfaces().Any(IsGenericConfigSource))
                .ToList().ForEach(t => Configurer.ConfigureComponent(t, DependencyLifecycle.InstancePerCall));
        }

        /// <summary>
        ///     Creates a new configuration object scanning assemblies in the regular runtime directory.
        /// </summary>
        public static Configure With()
        {
            return With(o => { });
        }


        /// <summary>
        ///     Initializes the endpoint configuration with the specified customizations.
        /// </summary>
        /// <param name="customizations">The customizations builder.</param>
        /// <returns>A new endpoint configuration.</returns>
        public static Configure With(Action<ConfigurationBuilder> customizations)
        {
            var options = new ConfigurationBuilder();

            customizations(options);

            return options.BuildConfiguration();
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

            Address.InitializeLocalAddress(Settings.EndpointName());

            WireUpConfigSectionOverrides();

            featureActivator = new FeatureActivator(Settings);

            Configurer.RegisterSingleton(featureActivator);

            ForAllTypes<Feature>(TypesToScan, t => featureActivator.Add(t.Construct<Feature>()));

            ForAllTypes<IWantToRunWhenConfigurationIsComplete>(TypesToScan, t => Configurer.ConfigureComponent(t, DependencyLifecycle.InstancePerCall));

            ForAllTypes<IWantToRunWhenBusStartsAndStops>(TypesToScan, t => Configurer.ConfigureComponent(t, DependencyLifecycle.InstancePerCall));

            ActivateAndInvoke<IWantToRunBeforeConfigurationIsFinalized>(t => t.Run(this));

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
        internal static void ForAllTypes<T>(IList<Type> types, Action<Type> action) where T : class
        {
            // ReSharper disable HeapView.SlowDelegateCreation
            types.Where(t => typeof(T).IsAssignableFrom(t) && !(t.IsAbstract || t.IsInterface))
                // ReSharper restore HeapView.SlowDelegateCreation
                .ToList().ForEach(action);
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

            ForAllTypes<T>(TypesToScan, t =>
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

        FeatureActivator featureActivator;
        bool initialized;
        ILog logger = LogManager.GetLogger<Configure>();
    }
}