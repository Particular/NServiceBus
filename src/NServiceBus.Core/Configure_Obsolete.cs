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
        /// <summary>
        /// Gets/sets the object used to configure components.
        /// This object should eventually reference the same container as the Builder.
        /// </summary>
        [ObsoleteEx(Replacement = "Configure.With(c => c.RegisterComponent(r => r... ))", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public IConfigureComponents Configurer
        {
            get
            {
                throw new InvalidOperationException();
            }
            // ReSharper disable ValueParameterNotUsed
            set
            // ReSharper restore ValueParameterNotUsed
            {
            }
        }

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
            Replacement = "Configure.With(b => b.EndpointName('MyEndpoint'))")]
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
            Replacement = "Configure.With(o => o.CustomConfigurationSource(myConfigSource))")]
        public Configure CustomConfigurationSource(IConfigurationSource configurationSource)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "Configure.With(b => b.ScanAssembliesInDirectory(directoryToProbe))")]
        public static Configure With(string probeDirectory)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "Configure.With(b => b.AssembliesToScan(listOfAssemblies))")]
        public static Configure With(IEnumerable<Assembly> assemblies)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "Configure.With(b => b.AssembliesToScan(listOfAssemblies))")]
        public static Configure With(params Assembly[] assemblies)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "Configure.With(b => b.TypesToScan(listOfTypes));")]
        public static Configure With(IEnumerable<Type> typesToScan)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "Configure.With(o => o.EndpointName(myEndpointName))")]
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
            Replacement = "Configure.With(o => o.EndpointName(myEndpointName))")]
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
            Replacement = "Configure.With(b => b.EndpointName(myEndpointName));")]
        public static Func<string> GetEndpointNameAction;

        [ObsoleteEx(
           RemoveInVersion = "6",
           TreatAsErrorFromVersion = "5",
           Replacement = "Configure.With(b => b.UseSerialization<Binary>())")]
        public static SerializationSettings Serialization
        {
            get { throw new NotImplementedException(); }
        }
        
        [ObsoleteEx(
          Message = "This has been converted to extension methods",
          RemoveInVersion = "6",
          TreatAsErrorFromVersion = "5",
          Replacement = "Configure.With(b => b.EnableFeature<T>() or b.DisableFeature<T>())")]
        public static FeatureSettings Features
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        [ObsoleteEx(
            Message = "This has been converted to an extension method",
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "Configure.With(b => b.Transactions())")]
        public static TransactionSettings Transactions
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }
}

namespace NServiceBus.Features
{
    [ObsoleteEx(
          Message = "This has been converted to extension methods",
          RemoveInVersion = "6",
          TreatAsErrorFromVersion = "5",
          Replacement = "Configure.With(b => b.EnableFeature<T>() or b.DisableFeature<T>())")]
    public class FeatureSettings
    {
    }

}