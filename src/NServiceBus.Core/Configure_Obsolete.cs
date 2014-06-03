#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Config.ConfigurationSource;
    using Logging;
    using ObjectBuilder;

    public partial class Configure
    {
        public static Configure Instance
        {
            get
            {
                //we can't check for null here since that would break the way we do extension methods (the must be on a instance)
                return instance;
            }
        }

        static ILog Logger
        {
            get { return LogManager.GetLogger<Configure>(); }
        }

        /// <summary>
        ///     The name of this endpoint.
        /// </summary>
        [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5", Replacement = "config.Settings.EndpointName()")]
        public string EndpointName
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        ///     True if any of the <see cref="With()" /> has been called.
        /// </summary>
        static bool WithHasBeenCalled()
        {
            return instance != null;
        }

        /// <summary>
        ///     True if a builder has been defined.
        /// </summary>
        internal static bool BuilderIsConfigured()
        {
            if (!WithHasBeenCalled())
            {
                return false;
            }

            return Instance.HasBuilder();
        }

        /// <summary>
        ///     Configures the given type with the given lifecycle <see cref="DependencyLifecycle" />.
        /// </summary>
        public static IComponentConfig Component(Type type, DependencyLifecycle lifecycle)
        {
            if (Instance == null)
            {
                throw new InvalidOperationException("You need to call Configure.With() before calling Configure.Component()");
            }

            return Instance.Configurer.ConfigureComponent(type, lifecycle);
        }


        /// <summary>
        ///     Returns true if a component of type <typeparamref name="T" /> exists in the container.
        /// </summary>
        public static bool HasComponent<T>()
        {
            return HasComponent(typeof(T));
        }


        /// <summary>
        ///     Returns true if a component of type <paramref name="componentType" /> exists in the container.
        /// </summary>
        public static bool HasComponent(Type componentType)
        {
            if (Instance == null)
            {
                throw new InvalidOperationException("You need to call Configure.With() before calling Configure.HasComponent");
            }

            return Instance.Configurer.HasComponent(componentType);
        }

        /// <summary>
        ///     Sets the current configuration source.
        /// </summary>
        [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5", Replacement = "With(o => o.CustomConfigurationSource(myConfigSource))")]
        public Configure CustomConfigurationSource(IConfigurationSource configurationSource)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5", Replacement = "With(o => o.AssembliesInDirectory(probeDirectory))")]
        public static Configure With(string probeDirectory)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5", Replacement = "With(o => o.ScanAssemblies(assemblies))")]
        public static Configure With(IEnumerable<Assembly> assemblies)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5", Replacement = "With(o => o.ScanAssemblies(assemblies));")]
        public static Configure With(params Assembly[] assemblies)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5", Replacement = "With(o => o.ScanAssemblies(assemblies))")]
        public static Configure With(IEnumerable<Type> typesToScan)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5", Replacement = "With(o => o.EndpointName(definesEndpointName))")]
        public static Configure DefineEndpointName(Func<string> definesEndpointName)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Sets the function that specified the name of this endpoint
        /// </summary>
        [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5", Replacement = "With(o => o.EndpointName(name))")]
        public static Configure DefineEndpointName(string name)
        {
            throw new NotImplementedException();
        }

        static Configure instance;
    }
}