namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using NServiceBus.Config.ConfigurationSource;
    using NServiceBus.Features;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.ObjectBuilder.Common;
    using NServiceBus.Pipeline;
    using NServiceBus.Settings;
    using NServiceBus.Utils.Reflection;

    /// <summary>
    ///     Central configuration entry point.
    /// </summary>
    public class Configure
    {
        /// <summary>
        ///     Creates a new instance of <see cref="Configure"/>.
        /// </summary>
        public Configure(SettingsHolder settings, IContainer container, List<Action<IConfigureComponents>> registrations, PipelineSettings pipeline, Dictionary<string, string> outgoingHeaders)
        {
            Settings = settings;
            this.pipeline = pipeline;
            this.outgoingHeaders = outgoingHeaders;

            RegisterContainerAdapter(container);
            RunUserRegistrations(registrations);

            this.container.RegisterSingleton(this);
            this.container.RegisterSingleton<ReadOnlySettings>(settings);
        }

        /// <summary>
        ///     Endpoint wide outgoing headers to be added to all sent messages.
        /// </summary>
        public IDictionary<string, string> OutgoingHeaders
        {
            get { return outgoingHeaders = outgoingHeaders ?? new Dictionary<string, string>(); }
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
                registration(container);
            }
        }

        void RegisterContainerAdapter(IContainer container)
        {
            var b = new CommonObjectBuilder
            {
                Container = container,
            };

            Builder = b;
            this.container = b;

            this.container.ConfigureComponent<CommonObjectBuilder>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(c => c.Container, container);
        }

        void WireUpConfigSectionOverrides()
        {
            foreach (var t in TypesToScan.Where(t => t.GetInterfaces().Any(IsGenericConfigSource)))
            {
                container.ConfigureComponent(t, DependencyLifecycle.InstancePerCall);
            }
        }

        /// <summary>
        /// Returns the queue name of this endpoint.
        /// </summary>
        public string LocalAddress
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

            var featureActivator = new FeatureActivator(Settings);

            container.RegisterSingleton(featureActivator);

            ForAllTypes<Feature>(TypesToScan, t => featureActivator.Add(t.Construct<Feature>()));

            ForAllTypes<IWantToRunWhenBusStartsAndStops>(TypesToScan, t => container.ConfigureComponent(t, DependencyLifecycle.InstancePerCall));

            ActivateAndInvoke<IWantToRunBeforeConfigurationIsFinalized>(TypesToScan, t => t.Run(this));

            var featureStats = featureActivator.SetupFeatures(new FeatureConfigurationContext(this));

            pipeline.RegisterBehaviorsInContainer(Settings, container);

            container.RegisterSingleton(featureStats);

            featureActivator.RegisterStartupTasks(container);

            localAddress = Settings.LocalAddress();

            ReportFeatures(featureStats);
            StartFeatures(featureActivator);
        }

        static void ReportFeatures(FeaturesReport featureStats)
        {
            var reporter = new DisplayDiagnosticsForFeatures();
            reporter.Run(featureStats);
        }

        void StartFeatures(FeatureActivator featureActivator)
        {
            var featureRunner = new FeatureRunner(Builder, featureActivator);
            container.RegisterSingleton(featureRunner);

            featureRunner.Start();
        }

        /// <summary>
        ///     Applies the given action to all the scanned types that can be assigned to <typeparamref name="T" />.
        /// </summary>
        internal static void ForAllTypes<T>(IEnumerable<Type> types, Action<Type> action) where T : class
        {
            // ReSharper disable HeapView.SlowDelegateCreation
            foreach (var type in types.Where(t => typeof(T).IsAssignableFrom(t) && !(t.IsAbstract || t.IsInterface)))
            {
                action(type);
            }
            // ReSharper restore HeapView.SlowDelegateCreation
        }

        internal string PublicReturnAddress
        {
            get
            {
                if (!Settings.HasSetting("PublicReturnAddress"))
                {
                    return LocalAddress;
                }

                return Settings.Get<string>("PublicReturnAddress");
            }
        }

        internal static void ActivateAndInvoke<T>(IList<Type> types, Action<T> action) where T : class
        {
            ForAllTypes<T>(types, t =>
            {
                var instanceToInvoke = (T)Activator.CreateInstance(t);
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

        internal IConfigureComponents container;

        internal PipelineSettings pipeline;
        Dictionary<string, string> outgoingHeaders;

        //HACK: Set by the tests
        internal string localAddress;
    }
}
