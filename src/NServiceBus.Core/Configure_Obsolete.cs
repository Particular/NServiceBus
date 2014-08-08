#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using Config.ConfigurationSource;
    using Features;
    using ObjectBuilder;
    using Settings;

    public partial class Configure
    {

        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5.0",
            Message = "Configure is now instance based. For usages before the container is configured an instance of Configure is passed in. For usages after the container is configured then an instance of Configure can be extracted from the container.")]
        public static Configure Instance
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "config.Settings.EndpointName()")]
        public static string EndpointName
        {
            get { throw new NotImplementedException(); }
        }


        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5")]
        public static bool WithHasBeenCalled()
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "Simply execute this action instead of calling this method")]
        public Configure RunCustomAction(Action action)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5")]
        public static bool BuilderIsConfigured()
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "ReadOnlySettings.GetConfigSection<T>")]
        public static T GetConfigSection<T>()
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Configure is now instance based.",
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "configure.Configurer.ConfigureComponent")]
        public static IComponentConfig Component(Type type, DependencyLifecycle lifecycle)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Configure is now instance based.",
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "configure.Configurer.ConfigureComponent")]
        public static IComponentConfig<T> Component<T>(DependencyLifecycle lifecycle)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Configure is now instance based.",
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "configure.Configurer.ConfigureComponent")]
        public static IComponentConfig<T> Component<T>(Func<T> componentFactory, DependencyLifecycle lifecycle)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Configure is now instance based.",
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "configure.Configurer.ConfigureComponent")]
        public static IComponentConfig<T> Component<T>(Func<IBuilder, T> componentFactory, DependencyLifecycle lifecycle)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Configure is now instance based.",
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "configure.Configurer.HasComponent")]
        public static bool HasComponent<T>()
        {
            throw new NotImplementedException(); 
        }

        [ObsoleteEx(
    Message = "Configure is now instance based.",
    RemoveInVersion = "6",
    TreatAsErrorFromVersion = "5",
    Replacement = "configure.Configurer.HasComponent")]
        public static bool HasComponent(Type componentType)
        {
            throw new NotImplementedException(); 
        }

        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "var configure = Configure.Configure.With(o => o.CustomConfigurationSource(myConfigSource))")]
        public Configure CustomConfigurationSource(IConfigurationSource configurationSource)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "var configure = Configure.Configure.With(o => o.AssembliesInDirectory(probeDirectory))")]
        public static Configure With(string probeDirectory)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "var configure = Configure.With(o => o.ScanAssemblies(assemblies))")]
        public static Configure With(IEnumerable<Assembly> assemblies)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "var configure = Configure.With(o => o.ScanAssemblies(assemblies));")]
        public static Configure With(params Assembly[] assemblies)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "var configure = Configure.With(o => o.ScanAssemblies(assemblies))")]
        public static Configure With(IEnumerable<Type> typesToScan)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "var configure = Configure.With(o => o.EndpointName(definesEndpointName))")]
        public static Configure DefineEndpointName(Func<string> definesEndpointName)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Sets the function that specified the name of this endpoint
        /// </summary>
        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "var configure = Configure.With(o => o.EndpointName(name))")]
        public static Configure DefineEndpointName(string name)
        {
            throw new NotImplementedException();
        }
        
        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Message = "No longer an extension point for NSB")]
        public static Func<FileInfo, Assembly> LoadAssembly = s => Assembly.LoadFrom(s.FullName);

        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "var configure = Configure.With(b => b.EndpointName(\"MyEndpointName\"));")]
        public static Func<string> GetEndpointNameAction;

        [ObsoleteEx(
           RemoveInVersion = "6",
           TreatAsErrorFromVersion = "5",
           Replacement = "config.UseSerialization<TSerializer>(c=>c.AnyCustomSettingNeeded())")]
        public static SerializationSettings Serialization
        {
            get { throw new NotImplementedException(); }
        }

        //public static Func<string> DefineEndpointVersionRetriever;

        //public static IConfigurationSource ConfigurationSource{get;set;}

        //public static Endpoint Endpoint
        //{
        //    get
        //    {
        //    }
        //}

        [ObsoleteEx(
          Message = "This has been converted to extension methods",
          RemoveInVersion = "6",
          TreatAsErrorFromVersion = "5",
          Replacement = "configure.EnableFeature<T>() or configure.DisableFeature<T>()")]
        public static FeatureSettings Features
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        //public static SerializationSettings Serialization
        //{
        //    get
        //    {
        //    }
        //}

        [ObsoleteEx(
            Message = "This has been converted to an extension method",
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "configure.Transactions(Action<TransactionSettings>)")]
        public static TransactionSettings Transactions
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        //public static TransportSettings Transports
        //{
        //    get
        //    {
        //    }
        //}

        //public static IList<Type> TypesToScan
        //{
        //    get;
        //    private set;
        //}

    }

}

namespace NServiceBus.Features
{


    [ObsoleteEx(
          Message = "This has been converted to extension methods",
          RemoveInVersion = "6",
          TreatAsErrorFromVersion = "5",
          Replacement = "configure.EnableFeature<T>() or configure.DisableFeature<T>()")]
    public class FeatureSettings
    {
    }

}