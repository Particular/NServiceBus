namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
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
        ///     Creates a new instance of <see cref="Configure"/>.
        /// </summary>
        public Configure(SettingsHolder settings, IContainer container, List<Action<IConfigureComponents>> registrations, PipelineSettings pipeline)
        {
            Settings = settings;
            this.pipeline = pipeline;
            
            RegisterContainerAdapter(container);
            RunUserRegistrations(registrations);

            configurer.RegisterSingleton(this);
            configurer.RegisterSingleton<ReadOnlySettings>(settings);
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
                registration(configurer);
            }
        }

        void RegisterContainerAdapter(IContainer container)
        {
            var b = new CommonObjectBuilder
            {
                Container = container,
            };

            Builder = b;
            configurer = b;

            configurer.ConfigureComponent<CommonObjectBuilder>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(c => c.Container, container);
        }


        void WireUpConfigSectionOverrides()
        {
            TypesToScan
                .Where(t => t.GetInterfaces().Any(IsGenericConfigSource))
                .ToList().ForEach(t => configurer.ConfigureComponent(t, DependencyLifecycle.InstancePerCall));
        }

       
        /// <summary>
        /// Returns the queue name of this endpoint.
        /// </summary>
        public Address LocalAddress
        {
            get
            {
                Debug.Assert(localAddress != null);
                return localAddress;                
            }
        }

        internal void Initialize()
        {
            WireUpConfigSectionOverrides();

            featureActivator = new FeatureActivator(Settings);

            configurer.RegisterSingleton(featureActivator);

            ForAllTypes<Feature>(TypesToScan, t => featureActivator.Add(t.Construct<Feature>()));

            ForAllTypes<IWantToRunWhenConfigurationIsComplete>(TypesToScan, t => configurer.ConfigureComponent(t, DependencyLifecycle.InstancePerCall));

            ForAllTypes<IWantToRunWhenBusStartsAndStops>(TypesToScan, t => configurer.ConfigureComponent(t, DependencyLifecycle.InstancePerCall));

            ActivateAndInvoke<IWantToRunBeforeConfigurationIsFinalized>(TypesToScan, t => t.Run(this));

            var featureStats = featureActivator.SetupFeatures(new FeatureConfigurationContext(this));

            configurer.RegisterSingleton(featureStats);

            featureActivator.RegisterStartupTasks(configurer);

            localAddress = Address.Parse(Settings.Get<string>("NServiceBus.LocalAddress"));

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

        internal Address PublicReturnAddress
        {
            get
            {
                if (!Settings.HasSetting("PublicReturnAddress"))
                {
                    return LocalAddress;
                }

                return Settings.Get<Address>("PublicReturnAddress");
            }
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

        internal IConfigureComponents configurer;

        FeatureActivator featureActivator;
        
        internal PipelineSettings pipeline;

        //HACK: Set by the tests
        internal Address localAddress;
    }
}
