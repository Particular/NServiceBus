namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
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
        internal Configure(SettingsHolder settings, IContainer container, List<Action<IConfigureComponents>> registrations, PipelineSettings pipeline)
        {
            Settings = settings;
            Pipeline = pipeline;

            LogManager.HasConfigBeenInitialised = true;

            RegisterContainerAdapter(container);
            RunUserRegistrations(registrations);

            Configurer.RegisterSingleton(this);
            Configurer.RegisterSingleton<ReadOnlySettings>(settings);
        }

        internal PipelineSettings Pipeline { get; private set; }

        internal IConfigureComponents Configurer { get; private set; }

        /// <summary>
        ///     Provides access to the settings holder
        /// </summary>
        public SettingsHolder Settings { get; private set; }

        /// <summary>
        ///     Gets the builder.
        /// </summary>
        public IBuilder Builder { get; private set; }

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
        void Initialize()
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

            ActivateAndInvoke<IWantToRunBeforeConfigurationIsFinalized>(TypesToScan, t => t.Run(this));

            featureActivator.SetupFeatures(new FeatureConfigurationContext(this));
            featureActivator.RegisterStartupTasks(Configurer);

            Builder.BuildAll<IWantToRunWhenConfigurationIsComplete>()
                .ToList()
                .ForEach(o => o.Run(this));

            initialized = true;
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

        internal static void ActivateAndInvoke<T>(IList<Type> types, Action<T> action) where T : class
        {
            ForAllTypes<T>(types, t =>
            {
                var instanceToInvoke = (T) Activator.CreateInstance(t);
                action(instanceToInvoke);

            });
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