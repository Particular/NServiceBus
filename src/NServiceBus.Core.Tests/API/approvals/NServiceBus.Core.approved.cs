[assembly: ReleaseDateAttribute("2015-01-27", "2015-01-27")]
[assembly: System.CLSCompliantAttribute(true)]
[assembly: System.Runtime.CompilerServices.InternalsVisibleToAttribute(@"NServiceBus.Core.Tests, PublicKey=00240000048000009400000006020000002400005253413100040000010001007f16e21368ff041183fab592d9e8ed37e7be355e93323147a1d29983d6e591b04282e4da0c9e18bd901e112c0033925eb7d7872c2f1706655891c5c9d57297994f707d16ee9a8f40d978f064ee1ffc73c0db3f4712691b23bf596f75130f4ec978cf78757ec034625a5f27e6bb50c618931ea49f6f628fd74271c32959efb1c5")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleToAttribute(@"NServiceBus.Hosting.Tests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100dde965e6172e019ac82c2639ffe494dd2e7dd16347c34762a05732b492e110f2e4e2e1b5ef2d85c848ccfb671ee20a47c8d1376276708dc30a90ff1121b647ba3b7259a6bc383b2034938ef0e275b58b920375ac605076178123693c6c4f1331661a62eba28c249386855637780e3ff5f23a6d854700eaa6803ef48907513b92")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleToAttribute(@"NServiceBus.PerformanceTests, PublicKey=00240000048000009400000006020000002400005253413100040000010001007f16e21368ff041183fab592d9e8ed37e7be355e93323147a1d29983d6e591b04282e4da0c9e18bd901e112c0033925eb7d7872c2f1706655891c5c9d57297994f707d16ee9a8f40d978f064ee1ffc73c0db3f4712691b23bf596f75130f4ec978cf78757ec034625a5f27e6bb50c618931ea49f6f628fd74271c32959efb1c5")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleToAttribute(@"ReturnToSourceQueue, PublicKey=0024000004800000940000000602000000240000525341310004000001000100dde965e6172e019ac82c2639ffe494dd2e7dd16347c34762a05732b492e110f2e4e2e1b5ef2d85c848ccfb671ee20a47c8d1376276708dc30a90ff1121b647ba3b7259a6bc383b2034938ef0e275b58b920375ac605076178123693c6c4f1331661a62eba28c249386855637780e3ff5f23a6d854700eaa6803ef48907513b92")]
[assembly: System.Runtime.InteropServices.ComVisibleAttribute(false)]
[assembly: System.Runtime.Versioning.TargetFrameworkAttribute(".NETFramework,Version=v4.5", FrameworkDisplayName=".NET Framework 4.5")]

namespace NServiceBus
{
    
    public class Address : System.Runtime.Serialization.ISerializable
    {
        public static readonly NServiceBus.Address Self;
        public static readonly NServiceBus.Address Undefined;
        public Address(string queueName, string machineName) { }
        protected Address(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        [System.ObsoleteAttribute("Please inject an instance of `Configure` and call `Configure.LocalAddress` instea" +
            "d. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Address Local { get; }
        public string Machine { get; }
        public string Queue { get; }
        public override bool Equals(object obj) { }
        public override int GetHashCode() { }
        public static void IgnoreMachineName() { }
        [System.ObsoleteAttribute("Please use `ConfigureTransport<T>.LocalAddress(queue)` instead. Will be removed i" +
            "n version 6.0.0.", true)]
        public static void InitializeLocalAddress(string queue) { }
        public static void OverrideDefaultMachine(string machineName) { }
        [System.ObsoleteAttribute(@"Use `configuration.OverridePublicReturnAddress(address)`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static void OverridePublicReturnAddress(NServiceBus.Address address) { }
        public static NServiceBus.Address Parse(string destination) { }
        public NServiceBus.Address SubScope(string qualifier) { }
        public override string ToString() { }
    }
    public enum AddressMode
    {
        Local = 0,
        Remote = 1,
    }
    public class AllAssemblies : NServiceBus.IExcludesBuilder, NServiceBus.IIncludesBuilder, System.Collections.Generic.IEnumerable<System.Reflection.Assembly>, System.Collections.IEnumerable
    {
        public static NServiceBus.IExcludesBuilder Except(string assemblyExpression) { }
        public System.Collections.Generic.IEnumerator<System.Reflection.Assembly> GetEnumerator() { }
        public static NServiceBus.IIncludesBuilder Matching(string assemblyExpression) { }
    }
    public class static AutoSubscribeSettingsExtensions
    {
        public static NServiceBus.AutomaticSubscriptions.Config.AutoSubscribeSettings AutoSubscribe(this NServiceBus.BusConfiguration config) { }
    }
    public class BinarySerializer : NServiceBus.Serialization.SerializationDefinition
    {
        protected internal override System.Type ProvidedByFeature() { }
    }
    [System.ObsoleteAttribute(@"Use `configuration.UseSerialization<BinarySerializer>()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
    public class static BinarySerializerConfigurationExtensions
    {
        [System.ObsoleteAttribute(@"Use `configuration.UseSerialization<BinarySerializer>()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure Binary(this NServiceBus.Settings.SerializationSettings settings) { }
    }
    public class BsonSerializer : NServiceBus.Serialization.SerializationDefinition
    {
        protected internal override System.Type ProvidedByFeature() { }
    }
    public class static Bus
    {
        public static NServiceBus.IStartableBus Create(NServiceBus.BusConfiguration configuration) { }
        public static NServiceBus.ISendOnlyBus CreateSendOnly(NServiceBus.BusConfiguration configuration) { }
    }
    public class BusAsyncResultEventArgs : System.EventArgs
    {
        public string MessageId { get; set; }
        public NServiceBus.Unicast.BusAsyncResult Result { get; set; }
    }
    public class BusConfiguration : NServiceBus.Configuration.AdvanceExtensibility.ExposeSettings
    {
        public BusConfiguration() { }
        public NServiceBus.Pipeline.PipelineSettings Pipeline { get; }
        public void AssembliesToScan(System.Collections.Generic.IEnumerable<System.Reflection.Assembly> assemblies) { }
        public void AssembliesToScan(params System.Reflection.Assembly[] assemblies) { }
        public NServiceBus.ConventionsBuilder Conventions() { }
        public void CustomConfigurationSource(NServiceBus.Config.ConfigurationSource.IConfigurationSource configurationSource) { }
        public void EndpointName(string name) { }
        [System.ObsoleteAttribute("This api does not do anything. Will be treated as an error from version 5.2.0. Wi" +
            "ll be removed in version 6.0.0.", false)]
        public void EndpointVersion(string version) { }
        public void OverrideLocalAddress(string queue) { }
        public void OverridePublicReturnAddress(NServiceBus.Address address) { }
        public void RegisterComponents(System.Action<NServiceBus.ObjectBuilder.IConfigureComponents> registration) { }
        public void ScanAssembliesInDirectory(string probeDirectory) { }
        public void TypesToScan(System.Collections.Generic.IEnumerable<System.Type> typesToScan) { }
        public void UseContainer<T>(System.Action<NServiceBus.Container.ContainerCustomizations> customizations = null)
            where T : NServiceBus.Container.ContainerDefinition, new () { }
        public void UseContainer(System.Type definitionType) { }
        public void UseContainer(NServiceBus.ObjectBuilder.Common.IContainer builder) { }
    }
    public class BusNotifications : System.IDisposable
    {
        public BusNotifications() { }
        public NServiceBus.Faults.ErrorsNotifications Errors { get; }
        public NServiceBus.Pipeline.PipelineNotifications Pipeline { get; }
    }
    public class CompletionResult
    {
        public int ErrorCode { get; set; }
        public object[] Messages { get; set; }
        public object State { get; set; }
    }
    public class static ConfigurationBuilderExtensions
    {
        public static void DisableFeature<T>(this NServiceBus.BusConfiguration config)
            where T : NServiceBus.Features.Feature { }
        public static void DisableFeature(this NServiceBus.BusConfiguration config, System.Type featureType) { }
        public static void EnableFeature<T>(this NServiceBus.BusConfiguration config)
            where T : NServiceBus.Features.Feature { }
        public static void EnableFeature(this NServiceBus.BusConfiguration config, System.Type featureType) { }
    }
    public class static ConfigurationTimeoutExtensions
    {
        public static void TimeToWaitBeforeTriggeringCriticalErrorOnTimeoutOutages(this NServiceBus.BusConfiguration config, System.TimeSpan timeToWait) { }
    }
    public class Configure
    {
        [System.ObsoleteAttribute(@"Use `configuration.EndpointName(myEndpointName)`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static System.Func<string> GetEndpointNameAction;
        [System.ObsoleteAttribute("No longer an extension point for NSB. Will be removed in version 6.0.0.", true)]
        public static System.Func<System.IO.FileInfo, System.Reflection.Assembly> LoadAssembly;
        public Configure(NServiceBus.Settings.SettingsHolder settings, NServiceBus.ObjectBuilder.Common.IContainer container, System.Collections.Generic.List<System.Action<NServiceBus.ObjectBuilder.IConfigureComponents>> registrations, NServiceBus.Pipeline.PipelineSettings pipeline) { }
        public NServiceBus.ObjectBuilder.IBuilder Builder { get; }
        [System.ObsoleteAttribute(@"Use `configuration.RegisterComponents(c => c.ConfigureComponent... ))`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public NServiceBus.ObjectBuilder.IConfigureComponents Configurer { get; set; }
        [System.ObsoleteAttribute(@"Use `configuration.EndpointName('MyEndpoint')`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static string EndpointName { get; }
        [System.ObsoleteAttribute(@"This has been converted to extension methods. Use `configuration.EnableFeature<T>()` or `configuration.DisableFeature<T>()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Features.FeatureSettings Features { get; }
        [System.ObsoleteAttribute(@"Configure is now instance based. For usages before the container is configured an instance of `Configure` is passed in. For usages after the container is configured then an instance of `Configure` can be extracted from the container. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure Instance { get; }
        public NServiceBus.Address LocalAddress { get; }
        [System.ObsoleteAttribute(@"Use `configuration.UseSerialization<BinarySerializer>())`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Settings.SerializationSettings Serialization { get; }
        public NServiceBus.Settings.SettingsHolder Settings { get; }
        [System.ObsoleteAttribute(@"This has been converted to an extension method. Use `configuration.Transactions()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Settings.TransactionSettings Transactions { get; }
        public System.Collections.Generic.IList<System.Type> TypesToScan { get; }
        [System.ObsoleteAttribute("Will be removed in version 6.0.0.", true)]
        public static bool BuilderIsConfigured() { }
        [System.ObsoleteAttribute("Configure is now instance based. Please use `configure.Configurer.ConfigureCompon" +
            "ent` instead. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.ObjectBuilder.IComponentConfig Component(System.Type type, NServiceBus.DependencyLifecycle lifecycle) { }
        [System.ObsoleteAttribute("Configure is now instance based. Please use `configure.Configurer.ConfigureCompon" +
            "ent` instead. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.ObjectBuilder.IComponentConfig<T> Component<T>(NServiceBus.DependencyLifecycle lifecycle) { }
        [System.ObsoleteAttribute("Configure is now instance based. Please use `configure.Configurer.ConfigureCompon" +
            "ent` instead. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.ObjectBuilder.IComponentConfig<T> Component<T>(System.Func<T> componentFactory, NServiceBus.DependencyLifecycle lifecycle) { }
        [System.ObsoleteAttribute("Configure is now instance based. Please use `configure.Configurer.ConfigureCompon" +
            "ent` instead. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.ObjectBuilder.IComponentConfig<T> Component<T>(System.Func<NServiceBus.ObjectBuilder.IBuilder, T> componentFactory, NServiceBus.DependencyLifecycle lifecycle) { }
        [System.ObsoleteAttribute("Not needed, can safely be removed. Will be removed in version 6.0.0.", true)]
        public NServiceBus.IStartableBus CreateBus() { }
        [System.ObsoleteAttribute(@"Use `configuration.CustomConfigurationSource(myConfigSource)`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public NServiceBus.Configure CustomConfigurationSource(NServiceBus.Config.ConfigurationSource.IConfigurationSource configurationSource) { }
        [System.ObsoleteAttribute(@"Use `configuration.EndpointName(myEndpointName)`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure DefineEndpointName(System.Func<string> definesEndpointName) { }
        [System.ObsoleteAttribute(@"Use `configuration.EndpointName(myEndpointName)`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure DefineEndpointName(string name) { }
        [System.ObsoleteAttribute("Please use `ReadOnlySettings.GetConfigSection<T>` instead. Will be removed in ver" +
            "sion 6.0.0.", true)]
        public static T GetConfigSection<T>() { }
        [System.ObsoleteAttribute("Configure is now instance based. Please use `configure.Configurer.HasComponent` i" +
            "nstead. Will be removed in version 6.0.0.", true)]
        public static bool HasComponent<T>() { }
        [System.ObsoleteAttribute("Configure is now instance based. Please use `configure.Configurer.HasComponent` i" +
            "nstead. Will be removed in version 6.0.0.", true)]
        public static bool HasComponent(System.Type componentType) { }
        [System.ObsoleteAttribute("Simply execute this action instead of calling this method. Will be removed in ver" +
            "sion 6.0.0.", true)]
        public NServiceBus.Configure RunCustomAction(System.Action action) { }
        [System.ObsoleteAttribute("Please use `Bus.Create(new BusConfiguration())` instead. Will be removed in versi" +
            "on 6.0.0.", true)]
        public static NServiceBus.Configure With() { }
        [System.ObsoleteAttribute(@"Use `configuration.ScanAssembliesInDirectory(directoryToProbe)`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure With(string probeDirectory) { }
        [System.ObsoleteAttribute(@"Use `configuration.AssembliesToScan(listOfAssemblies)`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure With(System.Collections.Generic.IEnumerable<System.Reflection.Assembly> assemblies) { }
        [System.ObsoleteAttribute(@"Use `configuration.AssembliesToScan(listOfAssemblies)`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure With(params System.Reflection.Assembly[] assemblies) { }
        [System.ObsoleteAttribute(@"Use `configuration.TypesToScan(listOfTypes)`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure With(System.Collections.Generic.IEnumerable<System.Type> typesToScan) { }
        [System.ObsoleteAttribute("Will be removed in version 6.0.0.", true)]
        public static bool WithHasBeenCalled() { }
    }
    [System.ObsoleteAttribute(@"Use `configuration.UseSerialization<BinarySerializer>()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
    public class static ConfigureBinarySerializer
    {
        [System.ObsoleteAttribute(@"Use `configuration.UseSerialization<BinarySerializer>()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure BinarySerializer(this NServiceBus.Configure config) { }
    }
    public class static ConfigureCriticalErrorAction
    {
        [System.ObsoleteAttribute("Use `configuration.DefineCriticalErrorAction()`, where configuration is an instan" +
            "ce of type `BusConfiguration`. Please use `ConfigureCriticalErrorAction.DefineCr" +
            "iticalErrorAction()` instead. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure DefineCriticalErrorAction(this NServiceBus.Configure config, System.Action<string, System.Exception> onCriticalError) { }
        public static void DefineCriticalErrorAction(this NServiceBus.BusConfiguration busConfiguration, System.Action<string, System.Exception> onCriticalError) { }
        [System.ObsoleteAttribute("Inject an instace of `CriticalError` and call `CriticalError.Raise`. Will be remo" +
            "ved in version 6.0.0.", true)]
        public static void RaiseCriticalError(string errorMessage, System.Exception exception) { }
    }
    [System.ObsoleteAttribute("Default builder will be used automatically. It is safe to remove this code. Will " +
        "be removed in version 6.0.0.", true)]
    public class static ConfigureDefaultBuilder
    {
        [System.ObsoleteAttribute("Default builder will be used automatically. It is safe to remove this code. Will " +
            "be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure DefaultBuilder(this NServiceBus.Configure config) { }
    }
    [System.ObsoleteAttribute("The NServiceBus Distributor was moved into its own assembly (NServiceBus.Distribu" +
        "tor.MSMQ.dll), please make sure you reference the new assembly. Will be removed " +
        "in version 6.0.0.", true)]
    public class static ConfigureDistributor
    {
        public static bool DistributorConfiguredToRunOnThisEndpoint(this NServiceBus.Configure config) { }
        public static bool DistributorEnabled(this NServiceBus.Configure config) { }
        public static NServiceBus.Configure EnlistWithDistributor(this NServiceBus.Configure config) { }
        public static NServiceBus.Configure RunDistributor(this NServiceBus.Configure config, bool withWorker = True) { }
        public static NServiceBus.Configure RunDistributorWithNoWorkerOnItsEndpoint(this NServiceBus.Configure config) { }
        public static bool WorkerRunsOnThisEndpoint(this NServiceBus.Configure config) { }
    }
    [System.ObsoleteAttribute("Will be removed in version 6.0.0.", true)]
    public class static ConfigureExtensions
    {
        [System.ObsoleteAttribute("Please use `Bus.CreateSendOnly(new BusConfiguration())` instead. Will be removed " +
            "in version 6.0.0.", true)]
        public static NServiceBus.IBus SendOnly(this NServiceBus.Configure config) { }
    }
    [System.ObsoleteAttribute("Will be removed in version 6.0.0.", true)]
    public class static ConfigureFaultsForwarder
    {
        [System.ObsoleteAttribute("It is safe to remove this method call. This is the default behavior. Will be remo" +
            "ved in version 6.0.0.", true)]
        public static NServiceBus.Configure MessageForwardingInCaseOfFault(this NServiceBus.Configure config) { }
    }
    public class static ConfigureFileShareDataBus
    {
        public static NServiceBus.DataBus.DataBusExtentions<NServiceBus.FileShareDataBus> BasePath(this NServiceBus.DataBus.DataBusExtentions<NServiceBus.FileShareDataBus> config, string basePath) { }
        [System.ObsoleteAttribute(@"Use `configuration.FileShareDataBus(basePath)`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Please use `ConfigureFileShareDataBus.FileShareDataBus(this BusConfiguration config, string basePath)` instead. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure FileShareDataBus(this NServiceBus.Configure config, string basePath) { }
        [System.ObsoleteAttribute(@"Use `configuration.UseDataBus<FileShareDataBus>().BasePath(basePath)`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be treated as an error from version 5.5.0. Will be removed in version 6.0.0.", false)]
        public static void FileShareDataBus(this NServiceBus.BusConfiguration config, string basePath) { }
    }
    public class static ConfigureHandlerSettings
    {
        public static void InitializeHandlerProperty<THandler>(this NServiceBus.BusConfiguration config, string property, object value) { }
    }
    public class static ConfigureInMemoryFaultManagement
    {
        public static void DiscardFailedMessagesInsteadOfSendingToErrorQueue(this NServiceBus.BusConfiguration config) { }
        [System.ObsoleteAttribute(@"Use `configuration.DiscardFailedMessagesInsteadOfSendingToErrorQueue()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure InMemoryFaultManagement(this NServiceBus.Configure config) { }
    }
    [System.ObsoleteAttribute("Use `configuration.UsePersistence<InMemoryPersistence>()`, where configuration is" +
        " an instance of type `BusConfiguration`. Will be removed in version 6.0.0.", true)]
    public class static ConfigureInMemorySagaPersister
    {
        [System.ObsoleteAttribute("Use `configuration.UsePersistence<InMemoryPersistence>()`, where configuration is" +
            " an instance of type `BusConfiguration`. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure InMemorySagaPersister(this NServiceBus.Configure config) { }
    }
    [System.ObsoleteAttribute("Use `configuration.UsePersistence<InMemoryPersistence>()`, where configuration is" +
        " an instance of type `BusConfiguration`. Will be removed in version 6.0.0.", true)]
    public class static ConfigureInMemorySubscriptionStorage
    {
        [System.ObsoleteAttribute("Use `configuration.UsePersistence<InMemoryPersistence>()`, where configuration is" +
            " an instance of type `BusConfiguration`. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure InMemorySubscriptionStorage(this NServiceBus.Configure config) { }
    }
    [System.ObsoleteAttribute("Use `configuration.UsePersistence<InMemoryPersistence>()`, where configuration is" +
        " an instance of type `BusConfiguration`. Will be removed in version 6.0.0.", true)]
    public class static ConfigureInMemoryTimeoutPersister
    {
        [System.ObsoleteAttribute("Use `configuration.UsePersistence<InMemoryPersistence>()`, where configuration is" +
            " an instance of type `BusConfiguration`. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure UseInMemoryTimeoutPersister(this NServiceBus.Configure config) { }
    }
    [System.ObsoleteAttribute(@"Use `configuration.UseSerialization<JsonSerializer>()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
    public class static ConfigureJsonSerializer
    {
        [System.ObsoleteAttribute(@"Use `configuration.UseSerialization<BsonSerializer>()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure BsonSerializer(this NServiceBus.Configure config) { }
        [System.ObsoleteAttribute(@"Use `configuration.UseSerialization<JsonSerializer>()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure JsonSerializer(this NServiceBus.Configure config) { }
    }
    public class static ConfigureLicenseExtensions
    {
        [System.ObsoleteAttribute("Use `configuration.License(licenseText)`, where configuration is an instance of t" +
            "ype `BusConfiguration`. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure License(this NServiceBus.Configure config, string licenseText) { }
        public static void License(this NServiceBus.BusConfiguration config, string licenseText) { }
        [System.ObsoleteAttribute("Use `configuration.LicensePath(licenseFile)`, where configuration is an instance " +
            "of type `BusConfiguration`. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure LicensePath(this NServiceBus.Configure config, string licenseFile) { }
        public static void LicensePath(this NServiceBus.BusConfiguration config, string licenseFile) { }
    }
    [System.ObsoleteAttribute("The NServiceBus Distributor was moved into its own assembly (NServiceBus.Distribu" +
        "tor.MSMQ.dll), please make sure you reference the new assembly. Will be removed " +
        "in version 6.0.0.", true)]
    public class static ConfigureMasterNode
    {
        public static NServiceBus.Configure AsMasterNode(this NServiceBus.Configure config) { }
        public static string GetMasterNode(this NServiceBus.Configure config) { }
        public static NServiceBus.Address GetMasterNodeAddress(this NServiceBus.Configure config) { }
        public static bool HasMasterNode(this NServiceBus.Configure config) { }
        public static bool IsConfiguredAsMasterNode(this NServiceBus.Configure config) { }
    }
    [System.ObsoleteAttribute(@"Please use 'UsingTransport<MsmqTransport>' on your 'IConfigureThisEndpoint' class or use `configuration.UseTransport<MsmqTransport>()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
    public class static ConfigureMsmqMessageQueue
    {
        [System.ObsoleteAttribute(@"Please use 'UsingTransport<MsmqTransport>' on your 'IConfigureThisEndpoint' class or use `configuration.UseTransport<MsmqTransport>()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure MsmqTransport(this NServiceBus.Configure config) { }
    }
    [System.ObsoleteAttribute(@"Use `configuration.UsePersistence<MsmqPersistence>()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
    public class static ConfigureMsmqSubscriptionStorage
    {
        [System.ObsoleteAttribute("Assign the queue name via `MsmqSubscriptionStorageConfig` section. Will be remove" +
            "d in version 6.0.0.", true)]
        public static NServiceBus.Address Queue { get; set; }
        [System.ObsoleteAttribute(@"Use configuration.UsePersistence<MsmqPersistence>(), where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure MsmqSubscriptionStorage(this NServiceBus.Configure config) { }
        [System.ObsoleteAttribute("Use `configuration.UsePersistence<MsmqPersistence>()`, where `configuration` is a" +
            "n instance of type `BusConfiguration` and assign the queue name via `MsmqSubscri" +
            "ptionStorageConfig` section. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure MsmqSubscriptionStorage(this NServiceBus.Configure config, string endpointName) { }
    }
    public class static ConfigurePurging
    {
        [System.ObsoleteAttribute("The `ReadOnlySettings` extension method `ConfigurePurging.GetPurgeOnStartup`. Wil" +
            "l be removed in version 6.0.0.", true)]
        public static bool PurgeRequested { get; }
        [System.ObsoleteAttribute(@"Use `configuration.PurgeOnStartup()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure PurgeOnStartup(this NServiceBus.Configure config, bool value) { }
        public static void PurgeOnStartup(this NServiceBus.BusConfiguration config, bool value) { }
        public static bool PurgeOnStartup(this NServiceBus.Configure config) { }
    }
    public class static ConfigureQueueCreation
    {
        [System.ObsoleteAttribute("Will be removed in version 6.0.0.", true)]
        public static bool DontCreateQueues { get; }
        public static bool CreateQueues(this NServiceBus.Configure config) { }
        [System.ObsoleteAttribute(@"Use `configuration.DoNotCreateQueues()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure DoNotCreateQueues(this NServiceBus.Configure config) { }
        public static void DoNotCreateQueues(this NServiceBus.BusConfiguration config) { }
    }
    [System.ObsoleteAttribute("RavenDB has been moved to its own stand alone nuget \'NServiceBus.RavenDB\'. Will b" +
        "e removed in version 6.0.0.", true)]
    public class static ConfigureRavenPersistence
    {
        [System.ObsoleteAttribute("RavenDB has been moved to its own stand alone nuget \'NServiceBus.RavenDB\'. Will b" +
            "e removed in version 6.0.0.", true)]
        public static NServiceBus.Configure CustomiseRavenPersistence(this NServiceBus.Configure config, object callback) { }
        [System.ObsoleteAttribute(@"RavenDB has been moved to its own stand alone nuget 'NServiceBus.RavenDB'. Install the nuget package. Use `configuration.UsePersistence<RavenDBPersistence>().SetMessageToDatabaseMappingConvention(convention)`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure MessageToDatabaseMappingConvention(this NServiceBus.Configure config, System.Func<NServiceBus.IMessageContext, string> convention) { }
        [System.ObsoleteAttribute(@"RavenDB has been moved to its own stand alone nuget 'NServiceBus.RavenDB'. Install the nuget package.` Use `configuration.UsePersistence<RavenDBPersistence>()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure RavenPersistence(this NServiceBus.Configure config) { }
        [System.ObsoleteAttribute(@"RavenDB has been moved to its own stand alone nuget 'NServiceBus.RavenDB'. Install the nuget package. Use `configuration.UsePersistence<RavenDBPersistence>().SetDefaultDocumentStore(...)`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure RavenPersistence(this NServiceBus.Configure config, string connectionStringName) { }
        [System.ObsoleteAttribute(@"RavenDB has been moved to its own stand alone nuget 'NServiceBus.RavenDB'. Install the nuget package. Use `configuration.UsePersistence<RavenDBPersistence>().SetDefaultDocumentStore(...)`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure RavenPersistence(this NServiceBus.Configure config, string connectionStringName, string database) { }
        [System.ObsoleteAttribute(@"RavenDB has been moved to its own stand alone nuget 'NServiceBus.RavenDB'. Install the nuget package. Use `configuration.UsePersistence<RavenDBPersistence>().SetDefaultDocumentStore(...)`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure RavenPersistence(this NServiceBus.Configure config, System.Func<string> getConnectionString) { }
        [System.ObsoleteAttribute(@"RavenDB has been moved to its own stand alone nuget 'NServiceBus.RavenDB'. Install the nuget package. Use `configuration.UsePersistence<RavenDBPersistence>().SetDefaultDocumentStore(...)`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure RavenPersistence(this NServiceBus.Configure config, System.Func<string> getConnectionString, string database) { }
        [System.ObsoleteAttribute(@"RavenDB has been moved to its own stand alone nuget 'NServiceBus.RavenDB'. Install the nuget package. Use `configuration.UsePersistence<RavenDBPersistence>().SetDefaultDocumentStore(documentStore)`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure RavenPersistenceWithStore(this NServiceBus.Configure config, object documentStore) { }
        [System.ObsoleteAttribute(@"RavenDB has been moved to its own stand alone nuget 'NServiceBus.RavenDB'. Install the nuget package. Use `configuration.UsePersistence<RavenDBPersistence>()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static void RegisterDefaults() { }
    }
    [System.ObsoleteAttribute("RavenDB has been moved to its own stand alone nuget \'NServiceBus.RavenDB\'. Will b" +
        "e removed in version 6.0.0.", true)]
    public class static ConfigureRavenSagaPersister
    {
        [System.ObsoleteAttribute(@"RavenDB has been moved to its own stand alone nuget 'NServiceBus.RavenDB'. Install the nuget package. Use `configuration.UsePersistence<RavenDBPersistence>().For(Storage.Sagas)`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure RavenSagaPersister(this NServiceBus.Configure config) { }
    }
    [System.ObsoleteAttribute("RavenDB has been moved to its own stand alone nuget \'NServiceBus.RavenDB\'. Will b" +
        "e removed in version 6.0.0.", true)]
    public class static ConfigureRavenSubscriptionStorage
    {
        [System.ObsoleteAttribute(@"RavenDB has been moved to its own stand alone nuget 'NServiceBus.RavenDB'. Install the nuget package. Use `configuration.UsePersistence<RavenDBPersistence>().For(Storage.Subscriptions)`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure RavenSubscriptionStorage(this NServiceBus.Configure config) { }
    }
    public class static ConfigureRijndaelEncryptionService
    {
        public static void RegisterEncryptionService(this NServiceBus.BusConfiguration config, System.Func<NServiceBus.ObjectBuilder.IBuilder, NServiceBus.Encryption.IEncryptionService> func) { }
        [System.ObsoleteAttribute("Use `configuration.RijndaelEncryptionService()`, where configuration is an instan" +
            "ce of type `BusConfiguration`. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure RijndaelEncryptionService(this NServiceBus.Configure config) { }
        public static void RijndaelEncryptionService(this NServiceBus.BusConfiguration config) { }
        public static void RijndaelEncryptionService(this NServiceBus.BusConfiguration config, string encryptionKey, System.Collections.Generic.List<string> expiredKeys = null) { }
    }
    [System.ObsoleteAttribute("Will be removed in version 6.0.0.", true)]
    public class static ConfigureSettingLocalAddressNameAction
    {
        [System.ObsoleteAttribute(@"Queue name is controlled by the endpoint name. The endpoint name can be configured using a `EndpointNameAttribute`, by passing a serviceName parameter to the host or calling `BusConfiguration.EndpointName` in the fluent API. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure DefineLocalAddressNameFunc(this NServiceBus.Configure config, System.Func<string> setLocalAddressNameFunc) { }
    }
    [System.ObsoleteAttribute("Will be removed in version 6.0.0.", true)]
    public class static ConfigureTimeoutManager
    {
        [System.ObsoleteAttribute(@"Use `configuration.DisableFeature<TimeoutManager>()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure DisableTimeoutManager(this NServiceBus.Configure config) { }
        [System.ObsoleteAttribute(@"Use `configuration.UsePersistence<InMemoryPersistence>()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure UseInMemoryTimeoutPersister(this NServiceBus.Configure config) { }
        [System.ObsoleteAttribute(@"RavenDB has been moved to its own stand alone nuget 'NServiceBus.RavenDB'. Install the nuget package. Use `configuration.UsePersistence<RavenDBPersistence>().For(Storage.Timeouts)`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure UseRavenTimeoutPersister(this NServiceBus.Configure config) { }
    }
    public class static ConfigureTransportConnectionString
    {
        public static string TransportConnectionString(this NServiceBus.Configure config) { }
    }
    [System.ObsoleteAttribute("Will be removed in version 6.0.0.", true)]
    public class static ConfigureUnicastBus
    {
        [System.ObsoleteAttribute("Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Address GetTimeoutManagerAddress(this NServiceBus.Configure config) { }
        [System.ObsoleteAttribute(@"UnicastBus is now the default and hence calling this method is redundant. `Bus.Create(configuration)`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure UnicastBus(this NServiceBus.Configure config) { }
    }
    [System.ObsoleteAttribute(@"Use `configuration.UseSerialization<XmlSerializer>()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
    public class static ConfigureXmlSerializer
    {
        [System.ObsoleteAttribute(@"Use `configuration.UseSerialization<XmlSerializer>()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure XmlSerializer(this NServiceBus.Configure config, string nameSpace = null, bool sanitizeInput = False) { }
    }
    public class static ContentTypes
    {
        public const string Binary = "application/binary";
        public const string Bson = "application/bson";
        public const string Json = "application/json";
        public const string Xml = "text/xml";
    }
    public class Conventions
    {
        public Conventions() { }
        public void AddSystemMessagesConventions(System.Func<System.Type, bool> definesMessageType) { }
        public System.TimeSpan GetTimeToBeReceived(System.Type messageType) { }
        public bool IsCommandType(System.Type t) { }
        public bool IsDataBusProperty(System.Reflection.PropertyInfo property) { }
        public bool IsEncryptedProperty(System.Reflection.PropertyInfo property) { }
        public bool IsEventType(System.Type t) { }
        public bool IsExpressMessageType(System.Type t) { }
        public bool IsInSystemConventionList(System.Type t) { }
        public bool IsMessageType(System.Type t) { }
    }
    public class ConventionsBuilder
    {
        public ConventionsBuilder() { }
        public NServiceBus.ConventionsBuilder DefiningCommandsAs(System.Func<System.Type, bool> definesCommandType) { }
        public NServiceBus.ConventionsBuilder DefiningDataBusPropertiesAs(System.Func<System.Reflection.PropertyInfo, bool> definesDataBusProperty) { }
        public NServiceBus.ConventionsBuilder DefiningEncryptedPropertiesAs(System.Func<System.Reflection.PropertyInfo, bool> definesEncryptedProperty) { }
        public NServiceBus.ConventionsBuilder DefiningEventsAs(System.Func<System.Type, bool> definesEventType) { }
        public NServiceBus.ConventionsBuilder DefiningExpressMessagesAs(System.Func<System.Type, bool> definesExpressMessageType) { }
        public NServiceBus.ConventionsBuilder DefiningMessagesAs(System.Func<System.Type, bool> definesMessageType) { }
        public NServiceBus.ConventionsBuilder DefiningTimeToBeReceivedAs(System.Func<System.Type, System.TimeSpan> retrieveTimeToBeReceived) { }
    }
    public class CriticalError
    {
        public CriticalError(System.Action<string, System.Exception> onCriticalErrorAction, NServiceBus.Configure configure) { }
        public void Raise(string errorMessage, System.Exception exception) { }
    }
    public class static CriticalTimeMonitoringConfig
    {
        public static void EnableCriticalTimePerformanceCounter(this NServiceBus.BusConfiguration config) { }
    }
    public class DataBusProperty<T> : NServiceBus.IDataBusProperty, System.Runtime.Serialization.ISerializable
        where T :  class
    {
        public DataBusProperty(T value) { }
        protected DataBusProperty(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public bool HasValue { get; set; }
        public string Key { get; set; }
        public T Value { get; }
        public void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public object GetValue() { }
        public void SetValue(object valueToSet) { }
    }
    public class static DateTimeExtensions
    {
        public static System.DateTime ToUtcDateTime(string wireFormattedString) { }
        public static string ToWireFormattedString(System.DateTime dateTime) { }
    }
    public enum DependencyLifecycle
    {
        SingleInstance = 0,
        InstancePerUnitOfWork = 1,
        InstancePerCall = 2,
    }
    public class static DurableMessagesConfig
    {
        public static void DisableDurableMessages(this NServiceBus.BusConfiguration config) { }
        public static bool DurableMessagesEnabled(this NServiceBus.Configure config) { }
        public static void EnableDurableMessages(this NServiceBus.BusConfiguration config) { }
    }
    public class EncryptedValue
    {
        public string Base64Iv { get; set; }
        public string EncryptedBase64Value { get; set; }
    }
    [System.ObsoleteAttribute(@"Use `configuration.EndpointName(myEndpointName)`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
    public class static EndpointConventions
    {
        [System.ObsoleteAttribute(@"Use `configuration.EndpointName(myEndpointName)`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure DefineEndpointName(this NServiceBus.Configure config, System.Func<string> definesEndpointName) { }
        [System.ObsoleteAttribute(@"Use `configuration.EndpointName(myEndpointName)`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure DefineEndpointName(this NServiceBus.Configure config, string name) { }
    }
    public sealed class EndpointNameAttribute : System.Attribute
    {
        public EndpointNameAttribute(string name) { }
        public string Name { get; set; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.All)]
    public sealed class EndpointSLAAttribute : System.Attribute
    {
        public EndpointSLAAttribute(string sla) { }
        public System.TimeSpan SLA { get; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Interface | System.AttributeTargets.All)]
    public sealed class ExpressAttribute : System.Attribute { }
    public class static ExtensionMethods
    {
        public static object CurrentMessageBeingHandled { get; set; }
        [System.ObsoleteAttribute("Please use `bus.GetMessageHeader(msg, key)` instead. Will be removed in version 6" +
            ".0.0.", true)]
        public static string GetHeader(this NServiceBus.IMessage msg, string key) { }
        public static string GetMessageHeader(this NServiceBus.IBus bus, object msg, string key) { }
        [System.ObsoleteAttribute("Please use `bus.SetMessageHeader(msg, key, value)` instead. Will be removed in ve" +
            "rsion 6.0.0.", true)]
        public static void SetHeader(this NServiceBus.IMessage msg, string key, string value) { }
        public static void SetMessageHeader(this NServiceBus.ISendOnlyBus bus, object msg, string key, string value) { }
    }
    public class FileShareDataBus : NServiceBus.DataBus.DataBusDefinition
    {
        protected internal override System.Type ProvidedByFeature() { }
    }
    public class First<T>
    
    {
        public First() { }
        public System.Collections.Generic.IEnumerable<System.Type> Types { get; }
        public NServiceBus.First<T> AndThen<K>() { }
        public static NServiceBus.First<T> Then<K>() { }
    }
    public class static Headers
    {
        public const string ContentType = "NServiceBus.ContentType";
        public const string ControlMessageHeader = "NServiceBus.ControlMessage";
        public const string ConversationId = "NServiceBus.ConversationId";
        public const string CorrelationId = "NServiceBus.CorrelationId";
        public const string DestinationSites = "NServiceBus.DestinationSites";
        public const string EnclosedMessageTypes = "NServiceBus.EnclosedMessageTypes";
        public const string FLRetries = "NServiceBus.FLRetries";
        public const string HasLicenseExpired = "$.diagnostics.license.expired";
        public const string HeaderName = "Header";
        public const string HostDisplayName = "$.diagnostics.hostdisplayname";
        public const string HostId = "$.diagnostics.hostid";
        public const string HttpFrom = "NServiceBus.From";
        public const string HttpTo = "NServiceBus.To";
        [System.ObsoleteAttribute(@"Enriching the headers for saga related information has been moved to the SagaAudit plugin in ServiceControl. Add a reference to the Saga audit plugin in your endpoint to get more information. Will be treated as an error from version 5.1.0. Will be removed in version 6.0.0.", false)]
        public const string InvokedSagas = "NServiceBus.InvokedSagas";
        public const string IsDeferredMessage = "NServiceBus.IsDeferredMessage";
        public const string IsSagaTimeoutMessage = "NServiceBus.IsSagaTimeoutMessage";
        public const string MessageId = "NServiceBus.MessageId";
        public const string MessageIntent = "NServiceBus.MessageIntent";
        public const string NServiceBusVersion = "NServiceBus.Version";
        public const string OriginatingAddress = "NServiceBus.OriginatingAddress";
        public const string OriginatingEndpoint = "NServiceBus.OriginatingEndpoint";
        public const string OriginatingHostId = "$.diagnostics.originating.hostid";
        public const string OriginatingMachine = "NServiceBus.OriginatingMachine";
        public const string OriginatingSagaId = "NServiceBus.OriginatingSagaId";
        public const string OriginatingSagaType = "NServiceBus.OriginatingSagaType";
        public const string OriginatingSite = "NServiceBus.OriginatingSite";
        public const string ProcessingEnded = "NServiceBus.ProcessingEnded";
        public const string ProcessingEndpoint = "NServiceBus.ProcessingEndpoint";
        public const string ProcessingMachine = "NServiceBus.ProcessingMachine";
        public const string ProcessingStarted = "NServiceBus.ProcessingStarted";
        public const string RelatedTo = "NServiceBus.RelatedTo";
        public const string ReplyToAddress = "NServiceBus.ReplyToAddress";
        public const string Retries = "NServiceBus.Retries";
        public const string ReturnMessageErrorCodeHeader = "NServiceBus.ReturnMessage.ErrorCode";
        public const string RouteTo = "NServiceBus.Header.RouteTo";
        public const string SagaId = "NServiceBus.SagaId";
        public const string SagaType = "NServiceBus.SagaType";
        public const string SubscriptionMessageType = "SubscriptionMessageType";
        public const string TimeSent = "NServiceBus.TimeSent";
        public const string WindowsIdentityName = "WinIdName";
        [System.ObsoleteAttribute("Please use `bus.GetMessageHeader(msg, key)` instead. Will be removed in version 6" +
            ".0.0.", true)]
        public static string GetMessageHeader(object msg, string key) { }
        [System.ObsoleteAttribute("Please use `bus.SetMessageHeader(msg, key, value)` instead. Will be removed in ve" +
            "rsion 6.0.0.", true)]
        public static void SetMessageHeader(object msg, string key, string value) { }
    }
    public class static HostInfoConfigurationExtensions
    {
        public static NServiceBus.HostInfoSettings UniquelyIdentifyRunningInstance(this NServiceBus.BusConfiguration config) { }
    }
    public class HostInfoSettings
    {
        public NServiceBus.HostInfoSettings UsingCustomIdentifier(System.Guid id) { }
        public NServiceBus.HostInfoSettings UsingInstalledFilePath() { }
        public NServiceBus.HostInfoSettings UsingNames(string instanceName, string hostName) { }
    }
    public interface IAuthorizeSubscriptions
    {
        bool AuthorizeSubscribe(string messageType, string clientEndpoint, System.Collections.Generic.IDictionary<string, string> headers);
        bool AuthorizeUnsubscribe(string messageType, string clientEndpoint, System.Collections.Generic.IDictionary<string, string> headers);
    }
    public interface IBus : NServiceBus.ISendOnlyBus, System.IDisposable
    {
        NServiceBus.IMessageContext CurrentMessageContext { get; }
        [System.ObsoleteAttribute("Removed to reduce complexity and API confusion. Will be treated as an error from " +
            "version 5.5.0. Will be removed in version 6.0.0.", false)]
        NServiceBus.IInMemoryOperations InMemory { get; }
        NServiceBus.ICallback Defer(System.TimeSpan delay, object message);
        NServiceBus.ICallback Defer(System.DateTime processAt, object message);
        void DoNotContinueDispatchingCurrentMessageToHandlers();
        void ForwardCurrentMessageTo(string destination);
        void HandleCurrentMessageLater();
        void Reply(object message);
        void Reply<T>(System.Action<T> messageConstructor);
        void Return<T>(T errorEnum);
        NServiceBus.ICallback SendLocal(object message);
        NServiceBus.ICallback SendLocal<T>(System.Action<T> messageConstructor);
        void Subscribe(System.Type messageType);
        void Subscribe<T>();
        void Unsubscribe(System.Type messageType);
        void Unsubscribe<T>();
    }
    [System.ObsoleteAttribute("Placeholder for obsoletes. Will be removed in version 6.0.0.", true)]
    public class static IBus_Obsoletes
    {
        [System.ObsoleteAttribute("Since multi message sends is obsoleted in v5 use `IBus.Send<T>()` instead. Will b" +
            "e removed in version 6.0.0.", true)]
        public static T CreateInstance<T>(this NServiceBus.IBus bus) { }
        [System.ObsoleteAttribute("Since multi message sends is obsoleted in v5 use `IBus.Send<T>()` instead. Will b" +
            "e removed in version 6.0.0.", true)]
        public static T CreateInstance<T>(this NServiceBus.IBus bus, System.Action<T> action) { }
        [System.ObsoleteAttribute("Since multi message sends is obsoleted in v5 use `IBus.Send<T>()` instead. Will b" +
            "e removed in version 6.0.0.", true)]
        public static object CreateInstance(this NServiceBus.IBus bus, System.Type messageType) { }
    }
    public interface ICallback
    {
        System.Threading.Tasks.Task<int> Register();
        System.Threading.Tasks.Task<T> Register<T>();
        System.Threading.Tasks.Task<T> Register<T>(System.Func<NServiceBus.CompletionResult, T> completion);
        System.Threading.Tasks.Task Register(System.Action<NServiceBus.CompletionResult> completion);
        System.IAsyncResult Register(System.AsyncCallback callback, object state);
        void Register<T>(System.Action<T> callback);
        void Register<T>(System.Action<T> callback, object synchronizer);
    }
    public interface ICommand : NServiceBus.IMessage { }
    [System.ObsoleteAttribute("Use the `NServiceBus.Hosting.Profiles.IConfigureLogging` interface which is conta" +
        "ined with in the `NServiceBus.Host` nuget. Will be removed in version 6.0.0.", true)]
    public interface IConfigureLogging { }
    [System.ObsoleteAttribute("Use the `NServiceBus.Hosting.Profiles.IConfigureLoggingForProfile<T>` interface w" +
        "hich is contained with in the `NServiceBus.Host` nuget. Will be removed in versi" +
        "on 6.0.0.", true)]
    public interface IConfigureLoggingForProfile<T> { }
    public interface IConfigureThisEndpoint
    {
        void Customize(NServiceBus.BusConfiguration configuration);
    }
    public interface IDataBusProperty
    {
        bool HasValue { get; set; }
        string Key { get; set; }
        object GetValue();
        void SetValue(object value);
    }
    public interface IEvent : NServiceBus.IMessage { }
    public interface IExcludesBuilder : System.Collections.Generic.IEnumerable<System.Reflection.Assembly>, System.Collections.IEnumerable
    {
        NServiceBus.IExcludesBuilder And(string assemblyExpression);
    }
    public interface IHandleMessages<T>
    
    {
        void Handle(T message);
    }
    public interface IIncludesBuilder : System.Collections.Generic.IEnumerable<System.Reflection.Assembly>, System.Collections.IEnumerable
    {
        NServiceBus.IIncludesBuilder And(string assemblyExpression);
        NServiceBus.IExcludesBuilder Except(string assemblyExpression);
    }
    [System.ObsoleteAttribute("Removed to reduce complexity and API confusion. Will be treated as an error from " +
        "version 5.5.0. Will be removed in version 6.0.0.", false)]
    public interface IInMemoryOperations { }
    public interface IManageMessageHeaders
    {
        System.Func<object, string, string> GetHeaderAction { get; }
        System.Action<object, string, string> SetHeaderAction { get; }
    }
    public interface IMessage { }
    public interface IMessageContext
    {
        System.Collections.Generic.IDictionary<string, string> Headers { get; }
        string Id { get; }
        NServiceBus.Address ReplyToAddress { get; }
    }
    public interface IMessageCreator
    {
        T CreateInstance<T>();
        T CreateInstance<T>(System.Action<T> action);
        object CreateInstance(System.Type messageType);
    }
    public interface INeedInitialization
    {
        void Customize(NServiceBus.BusConfiguration configuration);
    }
    public class InMemoryPersistence : NServiceBus.Persistence.PersistenceDefinition { }
    [System.ObsoleteAttribute("`IEnvironment` is no longer required instead use the non generic `INeedToInstallS" +
        "omething` and use `configuration.EnableInstallers()`, where `configuration` is a" +
        "n instance of type `BusConfiguration` to execute them. Will be removed in versio" +
        "n 6.0.0.", true)]
    public class static Install
    {
        [System.ObsoleteAttribute("`IEnvironment` is no longer required instead use the non generic `INeedToInstallS" +
            "omething` and use `configuration.EnableInstallers()`, where `configuration` is a" +
            "n instance of type `BusConfiguration` to execute them. Will be removed in versio" +
            "n 6.0.0.", true)]
        public static NServiceBus.Installer<T> ForInstallationOn<T>(this NServiceBus.Configure config) { }
        [System.ObsoleteAttribute("`IEnvironment` is no longer required instead use the non generic `INeedToInstallS" +
            "omething` and use `configuration.EnableInstallers()`, where `configuration` is a" +
            "n instance of type `BusConfiguration` to execute them. Will be removed in versio" +
            "n 6.0.0.", true)]
        public static NServiceBus.Installer<T> ForInstallationOn<T>(this NServiceBus.Configure config, string username) { }
    }
    public class static InstallConfigExtensions
    {
        public static void EnableInstallers(this NServiceBus.BusConfiguration config, string username = null) { }
        [System.ObsoleteAttribute("Use `configuration.EnableInstallers()`, where configuration is an instance of typ" +
            "e `BusConfiguration`. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure EnableInstallers(this NServiceBus.Configure config, string username = null) { }
    }
    [System.ObsoleteAttribute("`IEnvironment` is no longer required instead use the non generic `INeedToInstallS" +
        "omething` and use `configuration.EnableInstallers()`, where `configuration` is a" +
        "n instance of type `BusConfiguration` to execute them. Will be removed in versio" +
        "n 6.0.0.", true)]
    public class Installer<T> { }
    public interface ISendOnlyBus : System.IDisposable
    {
        System.Collections.Generic.IDictionary<string, string> OutgoingHeaders { get; }
        void Publish<T>(T message);
        void Publish<T>();
        void Publish<T>(System.Action<T> messageConstructor);
        NServiceBus.ICallback Send(object message);
        NServiceBus.ICallback Send<T>(System.Action<T> messageConstructor);
        NServiceBus.ICallback Send(string destination, object message);
        NServiceBus.ICallback Send(NServiceBus.Address address, object message);
        NServiceBus.ICallback Send<T>(string destination, System.Action<T> messageConstructor);
        NServiceBus.ICallback Send<T>(NServiceBus.Address address, System.Action<T> messageConstructor);
        NServiceBus.ICallback Send(string destination, string correlationId, object message);
        NServiceBus.ICallback Send(NServiceBus.Address address, string correlationId, object message);
        NServiceBus.ICallback Send<T>(string destination, string correlationId, System.Action<T> messageConstructor);
        NServiceBus.ICallback Send<T>(NServiceBus.Address address, string correlationId, System.Action<T> messageConstructor);
    }
    public interface ISpecifyMessageHandlerOrdering
    {
        void SpecifyOrder(NServiceBus.Order order);
    }
    public interface IStartableBus : NServiceBus.IBus, NServiceBus.ISendOnlyBus, System.IDisposable
    {
        NServiceBus.IBus Start();
    }
    [System.ObsoleteAttribute("Please use `INeedInitialization` or `IConfigureThisEndpoint`. Will be removed in " +
        "version 6.0.0.", true)]
    public interface IWantCustomInitialization
    {
        void Init();
    }
    [System.ObsoleteAttribute("Configure logging in the constructor of the class that implements IConfigureThisE" +
        "ndpoint. Will be removed in version 6.0.0.", true)]
    public interface IWantCustomLogging { }
    [System.ObsoleteAttribute("`IHandleProfile` is now passed an instance of `Configure`. `IWantCustomInitializa" +
        "tion` is now expected to return a new instance of `Configure`. Will be removed i" +
        "n version 6.0.0.", true)]
    public interface IWantTheEndpointConfig { }
    [System.ObsoleteAttribute(@"`IWantToRunBeforeConfiguration` is no longer in use. Please use the Feature concept instead and register a Default() in the ctor of your feature. If you used this to apply your own conventions please use use `configuration.Conventions().Defining...` , where configuration is an instance of type `BusConfiguration` available by implementing `IConfigureThisEndpoint` or `INeedInitialization`. Will be removed in version 6.0.0.", true)]
    public interface IWantToRunBeforeConfiguration
    {
        void Init(NServiceBus.Configure configure);
    }
    public interface IWantToRunBeforeConfigurationIsFinalized
    {
        void Run(NServiceBus.Configure config);
    }
    public interface IWantToRunWhenBusStartsAndStops
    {
        void Start();
        void Stop();
    }
    public class JsonSerializer : NServiceBus.Serialization.SerializationDefinition
    {
        protected internal override System.Type ProvidedByFeature() { }
    }
    public class static JsonSerializerConfigurationExtensions
    {
        [System.ObsoleteAttribute(@"Use `configuration.UseSerialization<BsonSerializer>()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Settings.SerializationSettings Bson(this NServiceBus.Settings.SerializationSettings settings) { }
        public static void Encoding(this NServiceBus.Serialization.SerializationExtentions<NServiceBus.JsonSerializer> config, System.Text.Encoding encoding) { }
        [System.ObsoleteAttribute(@"Use `configuration.UseSerialization<JsonSerializer>()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Settings.SerializationSettings Json(this NServiceBus.Settings.SerializationSettings settings) { }
    }
    public class static LoadMessageHandlersExtentions
    {
        [System.ObsoleteAttribute("It is safe to remove this method call. This is the default behavior. Will be remo" +
            "ved in version 6.0.0.", true)]
        public static NServiceBus.Configure LoadMessageHandlers(this NServiceBus.Configure config) { }
        [System.ObsoleteAttribute(@"Use `configuration.LoadMessageHandlers<TFirst>`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure LoadMessageHandlers<TFirst>(this NServiceBus.Configure config) { }
        [System.ObsoleteAttribute(@"Use `configuration.LoadMessageHandlers<T>`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure LoadMessageHandlers<T>(this NServiceBus.Configure config, NServiceBus.First<T> order) { }
        public static void LoadMessageHandlers<TFirst>(this NServiceBus.BusConfiguration config) { }
        public static void LoadMessageHandlers<T>(this NServiceBus.BusConfiguration config, NServiceBus.First<T> order) { }
    }
    [System.ObsoleteAttribute("Since the case where this exception was thrown should not be handled by consumers" +
        " of the API it has been removed. Will be removed in version 6.0.0.", true)]
    public class MessageConventionException : System.Exception { }
    [System.ObsoleteAttribute(@"Use `configuration.Conventions().DefiningMessagesAs(definesMessageType)`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
    public class static MessageConventions
    {
        [System.ObsoleteAttribute(@"Use `configuration.Conventions().DefiningCommandsAs(definesCommandType)`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure DefiningCommandsAs(this NServiceBus.Configure config, System.Func<System.Type, bool> definesCommandType) { }
        [System.ObsoleteAttribute(@"Use `configuration.Conventions().DefiningDataBusPropertiesAs(definesDataBusProperty)`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure DefiningDataBusPropertiesAs(this NServiceBus.Configure config, System.Func<System.Reflection.PropertyInfo, bool> definesDataBusProperty) { }
        [System.ObsoleteAttribute(@"Use `configuration.Conventions().DefiningEncryptedPropertiesAs(definesEncryptedProperty)`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure DefiningEncryptedPropertiesAs(this NServiceBus.Configure config, System.Func<System.Reflection.PropertyInfo, bool> definesEncryptedProperty) { }
        [System.ObsoleteAttribute(@"Use `configuration.Conventions().DefiningEventsAs(definesEventType)`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure DefiningEventsAs(this NServiceBus.Configure config, System.Func<System.Type, bool> definesEventType) { }
        [System.ObsoleteAttribute(@"Use `configuration.Conventions().DefiningExpressMessagesAs(definesExpressMessageType)`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure DefiningExpressMessagesAs(this NServiceBus.Configure config, System.Func<System.Type, bool> definesExpressMessageType) { }
        [System.ObsoleteAttribute(@"Use `configuration.Conventions().DefiningMessagesAs(definesMessageType)`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure DefiningMessagesAs(this NServiceBus.Configure config, System.Func<System.Type, bool> definesMessageType) { }
        [System.ObsoleteAttribute(@"Use `configuration.Conventions().DefiningTimeToBeReceivedAs(retrieveTimeToBeReceived)`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure DefiningTimeToBeReceivedAs(this NServiceBus.Configure config, System.Func<System.Type, System.TimeSpan> retrieveTimeToBeReceived) { }
    }
    public class MessageDeserializationException : System.Runtime.Serialization.SerializationException
    {
        public MessageDeserializationException(string transportMessageId, System.Exception innerException) { }
        protected MessageDeserializationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    [System.ObsoleteAttribute("Inject an instance of `IBus` in the constructor and assign that to a field for us" +
        "e. Will be removed in version 6.0.0.", true)]
    public class static MessageHandlerExtensionMethods
    {
        [System.ObsoleteAttribute("Inject an instance of `IBus` in the constructor and assign that to a field for us" +
            "e. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.IBus Bus<T>(this NServiceBus.IHandleMessages<T> handler) { }
    }
    public enum MessageIntentEnum
    {
        Send = 1,
        Publish = 2,
        Subscribe = 3,
        Unsubscribe = 4,
        Reply = 5,
    }
    [System.ObsoleteAttribute("Use `configuration.EnableCriticalTimePerformanceCounter()` or `configuration.Enab" +
        "leSLAPerformanceCounter(TimeSpan)`, where configuration is an instance of type `" +
        "BusConfiguration`. Will be removed in version 6.0.0.", true)]
    public class static MonitoringConfig
    {
        [System.ObsoleteAttribute("Use `configuration.EnableCriticalTimePerformanceCounter()`, where configuration i" +
            "s an instance of type `BusConfiguration`. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure EnablePerformanceCounters(this NServiceBus.Configure config) { }
        [System.ObsoleteAttribute("Use `configuration.EnableSLAPerformanceCounter(TimeSpan)`, where configuration is" +
            " an instance of type `BusConfiguration`. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure SetEndpointSLA(this NServiceBus.Configure config, System.TimeSpan sla) { }
    }
    public class MsmqTransport : NServiceBus.Transports.TransportDefinition
    {
        public MsmqTransport() { }
        protected internal override void Configure(NServiceBus.BusConfiguration config) { }
    }
    public class Order
    {
        public System.Collections.Generic.IEnumerable<System.Type> Types { get; set; }
        public void Specify<T>(NServiceBus.First<T> ordering) { }
        public void Specify(params System.Type[] priorityHandlers) { }
        public void SpecifyFirst<T>() { }
    }
    public class static OutboxConfigExtensions
    {
        public static NServiceBus.Outbox.OutboxSettings EnableOutbox(this NServiceBus.BusConfiguration config) { }
    }
    public class static PersistenceConfig
    {
        public static NServiceBus.PersistenceExtentions<T> UsePersistence<T>(this NServiceBus.BusConfiguration config)
            where T : NServiceBus.Persistence.PersistenceDefinition { }
        public static NServiceBus.PersistenceExtentions<T, S> UsePersistence<T, S>(this NServiceBus.BusConfiguration config)
            where T : NServiceBus.Persistence.PersistenceDefinition
            where S : NServiceBus.Persistence.StorageType { }
        public static NServiceBus.PersistenceExtentions UsePersistence(this NServiceBus.BusConfiguration config, System.Type definitionType) { }
    }
    public class PersistenceExtentions : NServiceBus.Configuration.AdvanceExtensibility.ExposeSettings
    {
        public PersistenceExtentions(System.Type definitionType, NServiceBus.Settings.SettingsHolder settings, System.Type storageType) { }
        [System.ObsoleteAttribute("Please use `UsePersistence<T, S>()` instead. Will be treated as an error from ver" +
            "sion 6.0.0. Will be removed in version 7.0.0.", false)]
        public NServiceBus.PersistenceExtentions For(params NServiceBus.Persistence.Storage[] specificStorages) { }
    }
    public class PersistenceExtentions<T> : NServiceBus.PersistenceExtentions
        where T : NServiceBus.Persistence.PersistenceDefinition
    {
        public PersistenceExtentions(NServiceBus.Settings.SettingsHolder settings) { }
        protected PersistenceExtentions(NServiceBus.Settings.SettingsHolder settings, System.Type storageType) { }
        [System.ObsoleteAttribute("Please use `UsePersistence<T, S>()` instead. Will be treated as an error from ver" +
            "sion 6.0.0. Will be removed in version 7.0.0.", false)]
        public NServiceBus.PersistenceExtentions<T> For(params NServiceBus.Persistence.Storage[] specificStorages) { }
    }
    public class PersistenceExtentions<T, S> : NServiceBus.PersistenceExtentions<T>
        where T : NServiceBus.Persistence.PersistenceDefinition
        where S : NServiceBus.Persistence.StorageType
    {
        public PersistenceExtentions(NServiceBus.Settings.SettingsHolder settings) { }
    }
    public class static ScaleOutExtentions
    {
        public static NServiceBus.Settings.ScaleOutSettings ScaleOut(this NServiceBus.BusConfiguration config) { }
        [System.ObsoleteAttribute(@"Use `configuration.ScaleOut().UseSingleBrokerQueue()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure ScaleOut(this NServiceBus.Configure config, System.Action<NServiceBus.Settings.ScaleOutSettings> customScaleOutSettings) { }
    }
    public class Schedule
    {
        public Schedule(NServiceBus.ObjectBuilder.IBuilder builder) { }
        [System.ObsoleteAttribute("Inject an instance of `Schedule` to your class and then call the non static membe" +
            "r `Schedule.Every(TimeSpan timeSpan, Action task)`. Will be removed in version 6" +
            ".0.0.", true)]
        public void Action(System.Action task) { }
        [System.ObsoleteAttribute("Inject an instance of `Schedule` to your class thenuse the non-static version of " +
            "`Schedule.Every(TimeSpan timeSpan, string name, Action task)`. Will be removed i" +
            "n version 6.0.0.", true)]
        public void Action(string name, System.Action task) { }
        [System.ObsoleteAttribute("Inject an instance of `Schedule` to your class and then call the non-static versi" +
            "on of `Schedule.Every(TimeSpan timeSpan, Action task)`. Will be removed in versi" +
            "on 6.0.0.", true)]
        public static NServiceBus.Schedule Every(System.TimeSpan timeSpan) { }
        public void Every(System.TimeSpan timeSpan, System.Action task) { }
        public void Every(System.TimeSpan timeSpan, string name, System.Action task) { }
    }
    public class static SecondLevelRetriesConfigExtensions
    {
        [System.ObsoleteAttribute(@"Use `configuration.SecondLevelRetries().CustomRetryPolicy()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure SecondLevelRetries(this NServiceBus.Configure config, System.Action<NServiceBus.SecondLevelRetries.Config.SecondLevelRetriesSettings> customSettings) { }
        public static NServiceBus.SecondLevelRetries.Config.SecondLevelRetriesSettings SecondLevelRetries(this NServiceBus.BusConfiguration config) { }
    }
    public class static SerializationConfigExtensions
    {
        public static NServiceBus.Serialization.SerializationExtentions<T> UseSerialization<T>(this NServiceBus.BusConfiguration config)
            where T : NServiceBus.Serialization.SerializationDefinition { }
        public static void UseSerialization(this NServiceBus.BusConfiguration config, System.Type serializerType) { }
    }
    [System.ObsoleteAttribute("Log4Net and Nlog integration has been moved to a stand alone nugets, \'NServiceBus" +
        ".Log4Net\' and \'NServiceBus.NLog\'. Will be removed in version 6.0.0.", true)]
    public class static SetLoggingLibrary
    {
        [System.ObsoleteAttribute("Please use `LogManager.UseFactory(ILoggerFactory)` instead. Will be removed in ve" +
            "rsion 6.0.0.", true)]
        public static void Custom(NServiceBus.Logging.ILoggerFactory loggerFactory) { }
        [System.ObsoleteAttribute("Log4Net integration has been moved to a stand alone nuget \'NServiceBus.Log4Net\'. " +
            "Install the \'NServiceBus.Log4Net\' nuget and run \'LogManager.Use<Log4NetFactory>(" +
            ");\'. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure Log4Net(this NServiceBus.Configure config) { }
        [System.ObsoleteAttribute("Log4Net integration has been moved to a stand alone nuget \'NServiceBus.Log4Net\'. " +
            "Install the \'NServiceBus.Log4Net\' nuget and run \'LogManager.Use<Log4NetFactory>(" +
            ");\'. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure Log4Net<TAppender>(this NServiceBus.Configure config, System.Action<TAppender> initializeAppender)
            where TAppender : new() { }
        [System.ObsoleteAttribute("Log4Net integration has been moved to a stand alone nuget \'NServiceBus.Log4Net\'. " +
            "Install the \'NServiceBus.Log4Net\' nuget and run \'LogManager.Use<Log4NetFactory>(" +
            ");\'. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure Log4Net(this NServiceBus.Configure config, object appenderSkeleton) { }
        [System.ObsoleteAttribute("Log4Net integration has been moved to a stand alone nuget \'NServiceBus.Log4Net\'. " +
            "Install the \'NServiceBus.Log4Net\' nuget and run \'LogManager.Use<Log4NetFactory>(" +
            ");\'. Will be removed in version 6.0.0.", true)]
        public static void Log4Net() { }
        [System.ObsoleteAttribute("Log4Net integration has been moved to a stand alone nuget \'NServiceBus.Log4Net\'. " +
            "Install the \'NServiceBus.Log4Net\' nuget and run \'LogManager.Use<Log4NetFactory>(" +
            ");\'. Will be removed in version 6.0.0.", true)]
        public static void Log4Net(System.Action config) { }
        [System.ObsoleteAttribute("Nlog integration has been moved to a stand alone nuget \'NServiceBus.NLog\'. Instal" +
            "l the \'NServiceBus.NLog\' nuget and run \'LogManager.Use<NLogFactory>();\'. Will be" +
            " removed in version 6.0.0.", true)]
        public static NServiceBus.Configure NLog(this NServiceBus.Configure config, params object[] targetsForNServiceBusToLogTo) { }
        [System.ObsoleteAttribute("Nlog integration has been moved to a stand alone nuget \'NServiceBus.NLog\'. Instal" +
            "l the \'NServiceBus.NLog\' nuget and run \'LogManager.Use<Log4NetFactory>();\'. Will" +
            " be removed in version 6.0.0.", true)]
        public static void NLog() { }
    }
    public class static SettingsExtentions
    {
        public static string EndpointName(this NServiceBus.Settings.ReadOnlySettings settings) { }
        public static System.Collections.Generic.IList<System.Type> GetAvailableTypes(this NServiceBus.Settings.ReadOnlySettings settings) { }
        public static T GetConfigSection<T>(this NServiceBus.Settings.ReadOnlySettings settings)
            where T :  class, new () { }
        public static NServiceBus.Address LocalAddress(this NServiceBus.Settings.ReadOnlySettings settings) { }
    }
    public class static SLAMonitoringConfig
    {
        public static void EnableSLAPerformanceCounter(this NServiceBus.BusConfiguration config, System.TimeSpan sla) { }
        public static void EnableSLAPerformanceCounter(this NServiceBus.BusConfiguration config) { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Interface | System.AttributeTargets.All)]
    public sealed class TimeToBeReceivedAttribute : System.Attribute
    {
        public TimeToBeReceivedAttribute(string timeSpan) { }
        public System.TimeSpan TimeToBeReceived { get; }
    }
    [System.ObsoleteAttribute(@"Use `configuration.Transactions().Enable()` or `configuration.Transactions().Disable()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
    public class static TransactionalConfigManager
    {
        [System.ObsoleteAttribute(@"Use `configuration.Transactions().Disable()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure DontUseTransactions(this NServiceBus.Configure config) { }
        [System.ObsoleteAttribute(@"Use `configuration.Transactions().IsolationLevel(IsolationLevel.Chaos)`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure IsolationLevel(this NServiceBus.Configure config, System.Transactions.IsolationLevel isolationLevel) { }
        [System.ObsoleteAttribute(@"Use `configuration.Transactions()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure IsTransactional(this NServiceBus.Configure config, bool value) { }
        [System.ObsoleteAttribute(@"Use `configuration.Transactions().DefaultTimeout(TimeSpan.FromMinutes(5))`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure TransactionTimeout(this NServiceBus.Configure config, System.TimeSpan transactionTimeout) { }
    }
    public class static TransactionSettingsExtentions
    {
        public static NServiceBus.Settings.TransactionSettings Transactions(this NServiceBus.BusConfiguration config) { }
    }
    [System.ObsoleteAttribute("Will be removed in version 6.0.0.", true)]
    public class TransportConfiguration
    {
        [System.ObsoleteAttribute(@"Use `configuration.UseTransport<T>().ConnectionString(connectionString)`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public void ConnectionString(string connectionString) { }
        [System.ObsoleteAttribute(@"Use` configuration.UseTransport<T>().ConnectionString(connectionString)`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public void ConnectionString(System.Func<string> connectionString) { }
        [System.ObsoleteAttribute(@"Use `configuration.UseTransport<T>().ConnectionStringName(name)`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public void ConnectionStringName(string name) { }
    }
    public class TransportExtensions : NServiceBus.Configuration.AdvanceExtensibility.ExposeSettings
    {
        public TransportExtensions(NServiceBus.Settings.SettingsHolder settings) { }
        public NServiceBus.TransportExtensions ConnectionString(string connectionString) { }
        public NServiceBus.TransportExtensions ConnectionString(System.Func<string> connectionString) { }
        public NServiceBus.TransportExtensions ConnectionStringName(string name) { }
    }
    public class TransportExtensions<T> : NServiceBus.TransportExtensions
        where T : NServiceBus.Transports.TransportDefinition
    {
        public TransportExtensions(NServiceBus.Settings.SettingsHolder settings) { }
        public new NServiceBus.TransportExtensions ConnectionString(string connectionString) { }
        public new NServiceBus.TransportExtensions ConnectionString(System.Func<string> connectionString) { }
        public new NServiceBus.TransportExtensions ConnectionStringName(string name) { }
    }
    public class TransportMessage
    {
        public TransportMessage() { }
        public TransportMessage(string existingId, System.Collections.Generic.Dictionary<string, string> existingHeaders) { }
        [System.ObsoleteAttribute("headers[Headers.ReplyToAddress]=replyToAddress; var tm = new TransportMessage(id," +
            "headers). Will be treated as an error from version 5.1.0. Will be removed in ver" +
            "sion 6.0.0.", false)]
        public TransportMessage(string existingId, System.Collections.Generic.Dictionary<string, string> existingHeaders, NServiceBus.Address replyToAddress) { }
        public byte[] Body { get; set; }
        public string CorrelationId { get; set; }
        public System.Collections.Generic.Dictionary<string, string> Headers { get; }
        public string Id { get; }
        public NServiceBus.MessageIntentEnum MessageIntent { get; set; }
        public bool Recoverable { get; set; }
        public NServiceBus.Address ReplyToAddress { get; }
        public System.TimeSpan TimeToBeReceived { get; set; }
    }
    [System.ObsoleteAttribute("Will be removed in version 6.0.0.", true)]
    public class static TransportReceiverConfig
    {
        [System.ObsoleteAttribute(@"Use `configuration.UseTransport(transportDefinitionType).ConnectionString()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure UseTransport<T>(this NServiceBus.Configure config, System.Func<string> definesConnectionString) { }
        [System.ObsoleteAttribute(@"Use `configuration.UseTransport(transportDefinitionType).ConnectionString()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure UseTransport(this NServiceBus.Configure config, System.Type transportDefinitionType, System.Func<string> definesConnectionString) { }
    }
    public class static UseDataBusExtensions
    {
        public static NServiceBus.DataBus.DataBusExtentions<T> UseDataBus<T>(this NServiceBus.BusConfiguration config)
            where T : NServiceBus.DataBus.DataBusDefinition, new () { }
        public static NServiceBus.DataBus.DataBusExtentions UseDataBus(this NServiceBus.BusConfiguration config, System.Type dataBusType) { }
    }
    public class static UseTransportExtensions
    {
        [System.ObsoleteAttribute(@"Use `configuration.UseTransport<T>()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure UseTransport<T>(this NServiceBus.Configure config, System.Action<NServiceBus.TransportConfiguration> customizations = null)
            where T : NServiceBus.Transports.TransportDefinition { }
        [System.ObsoleteAttribute(@"Use `configuration.UseTransport(transportDefinitionType)`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure UseTransport(this NServiceBus.Configure config, System.Type transportDefinitionType, System.Action<NServiceBus.TransportConfiguration> customizations = null) { }
        public static NServiceBus.TransportExtensions<T> UseTransport<T>(this NServiceBus.BusConfiguration busConfiguration)
            where T : NServiceBus.Transports.TransportDefinition, new () { }
        public static NServiceBus.TransportExtensions UseTransport(this NServiceBus.BusConfiguration busConfiguration, System.Type transportDefinitionType) { }
    }
    public class WireEncryptedString : System.Runtime.Serialization.ISerializable
    {
        public WireEncryptedString() { }
        public WireEncryptedString(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        [System.ObsoleteAttribute("No longer required. Will be treated as an error from version 6.0.0. Will be remov" +
            "ed in version 6.0.0.", false)]
        public string Base64Iv { get; set; }
        [System.ObsoleteAttribute("No longer required. Will be treated as an error from version 6.0.0. Will be remov" +
            "ed in version 6.0.0.", false)]
        public string EncryptedBase64Value { get; set; }
        public NServiceBus.EncryptedValue EncryptedValue { get; set; }
        public string Value { get; set; }
        public void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public class static XmlSerializationExtentions
    {
        public static NServiceBus.Serialization.SerializationExtentions<NServiceBus.XmlSerializer> DontWrapRawXml(this NServiceBus.Serialization.SerializationExtentions<NServiceBus.XmlSerializer> config) { }
        public static NServiceBus.Serialization.SerializationExtentions<NServiceBus.XmlSerializer> Namespace(this NServiceBus.Serialization.SerializationExtentions<NServiceBus.XmlSerializer> config, string namespaceToUse) { }
        public static NServiceBus.Serialization.SerializationExtentions<NServiceBus.XmlSerializer> SanitizeInput(this NServiceBus.Serialization.SerializationExtentions<NServiceBus.XmlSerializer> config) { }
    }
    public class XmlSerializer : NServiceBus.Serialization.SerializationDefinition
    {
        protected internal override System.Type ProvidedByFeature() { }
    }
    public class static XmlSerializerConfigurationExtensions
    {
        [System.ObsoleteAttribute(@"Use configuration.UseSerialization<XmlSerializer>(), where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure Xml(this NServiceBus.Settings.SerializationSettings settings, System.Action<NServiceBus.Serializers.XML.Config.XmlSerializationSettings> customSettings = null) { }
    }
}
namespace NServiceBus.AutomaticSubscriptions.Config
{
    
    public class AutoSubscribeSettings
    {
        public void AutoSubscribePlainMessages() { }
        public void DoNotAutoSubscribeSagas() { }
        public void DoNotRequireExplicitRouting() { }
    }
}
namespace NServiceBus.AutomaticSubscriptions
{
    
    [System.ObsoleteAttribute("Not an extension point any more. If you want full control over autosubscribe plea" +
        "se turn the feature off and implement your own for-loop calling Bus.Subscribe<Yo" +
        "urEvent>() when starting your endpoint. Will be removed in version 6.0.0.", true)]
    public interface IAutoSubscriptionStrategy
    {
        System.Collections.Generic.IEnumerable<System.Type> GetEventsToSubscribe();
    }
}
namespace NServiceBus.CircuitBreakers
{
    
    public class RepeatedFailuresOverTimeCircuitBreaker : System.IDisposable
    {
        public RepeatedFailuresOverTimeCircuitBreaker(string name, System.TimeSpan timeToWaitBeforeTriggering, System.Action<System.Exception> triggerAction) { }
        public RepeatedFailuresOverTimeCircuitBreaker(string name, System.TimeSpan timeToWaitBeforeTriggering, System.Action<System.Exception> triggerAction, System.TimeSpan delayAfterFailure) { }
        public void Dispose() { }
        public void Failure(System.Exception exception) { }
        public bool Success() { }
    }
}
namespace NServiceBus.Config
{
    
    public class AuditConfig : System.Configuration.ConfigurationSection
    {
        [System.Configuration.ConfigurationPropertyAttribute("OverrideTimeToBeReceived", IsRequired=false)]
        public System.TimeSpan OverrideTimeToBeReceived { get; set; }
        [System.Configuration.ConfigurationPropertyAttribute("QueueName", IsRequired=false)]
        public string QueueName { get; set; }
    }
    [System.ObsoleteAttribute("`IFinalizeConfiguration` is no longer in use. Please use the Feature concept inst" +
        "ead. Will be removed in version 6.0.0.", true)]
    public interface IFinalizeConfiguration
    {
        void FinalizeConfiguration(NServiceBus.Configure config);
    }
    public interface IWantToRunWhenConfigurationIsComplete
    {
        void Run(NServiceBus.Configure config);
    }
    public class Logging : System.Configuration.ConfigurationSection
    {
        [System.Configuration.ConfigurationPropertyAttribute("Threshold", DefaultValue="Info", IsRequired=true)]
        public string Threshold { get; set; }
    }
    public class MasterNodeConfig : System.Configuration.ConfigurationSection
    {
        [System.Configuration.ConfigurationPropertyAttribute("Node", IsRequired=true)]
        public string Node { get; set; }
    }
    public class MessageEndpointMapping : System.Configuration.ConfigurationElement, System.IComparable<NServiceBus.Config.MessageEndpointMapping>
    {
        [System.Configuration.ConfigurationPropertyAttribute("Assembly", IsRequired=false)]
        public string AssemblyName { get; set; }
        [System.Configuration.ConfigurationPropertyAttribute("Endpoint", IsRequired=true)]
        public string Endpoint { get; set; }
        [System.Configuration.ConfigurationPropertyAttribute("Messages", IsRequired=false)]
        public string Messages { get; set; }
        [System.Configuration.ConfigurationPropertyAttribute("Namespace", IsRequired=false)]
        public string Namespace { get; set; }
        [System.Configuration.ConfigurationPropertyAttribute("Type", IsRequired=false)]
        public string TypeFullName { get; set; }
        public int CompareTo(NServiceBus.Config.MessageEndpointMapping other) { }
        public void Configure(System.Action<System.Type, NServiceBus.Address> mapTypeToEndpoint) { }
    }
    public class MessageEndpointMappingCollection : System.Configuration.ConfigurationElementCollection
    {
        public new string AddElementName { get; set; }
        public new string ClearElementName { get; set; }
        public override System.Configuration.ConfigurationElementCollectionType CollectionType { get; }
        public new int Count { get; }
        public NServiceBus.Config.MessageEndpointMapping this[int index] { get; set; }
        public NServiceBus.Config.MessageEndpointMapping this[string Name] { get; }
        public new string RemoveElementName { get; }
        public void Add(NServiceBus.Config.MessageEndpointMapping mapping) { }
        protected override void BaseAdd(System.Configuration.ConfigurationElement element) { }
        public void Clear() { }
        protected override System.Configuration.ConfigurationElement CreateNewElement() { }
        protected override System.Configuration.ConfigurationElement CreateNewElement(string elementName) { }
        protected override object GetElementKey(System.Configuration.ConfigurationElement element) { }
        public int IndexOf(NServiceBus.Config.MessageEndpointMapping mapping) { }
        public override bool IsReadOnly() { }
        public void Remove(NServiceBus.Config.MessageEndpointMapping mapping) { }
        public void Remove(string name) { }
        public void RemoveAt(int index) { }
    }
    public class MessageForwardingInCaseOfFaultConfig : System.Configuration.ConfigurationSection
    {
        [System.Configuration.ConfigurationPropertyAttribute("ErrorQueue", IsRequired=true)]
        public string ErrorQueue { get; set; }
    }
    [System.ObsoleteAttribute("Use NServiceBus/Transport connectionString instead. Will be removed in version 6." +
        "0.0.", true)]
    public class MsmqMessageQueueConfig : System.Configuration.ConfigurationSection
    {
        [System.Configuration.ConfigurationPropertyAttribute("UseDeadLetterQueue", DefaultValue=true, IsRequired=false)]
        public bool UseDeadLetterQueue { get; set; }
        [System.Configuration.ConfigurationPropertyAttribute("UseJournalQueue", IsRequired=false)]
        public bool UseJournalQueue { get; set; }
    }
    public class MsmqSubscriptionStorageConfig : System.Configuration.ConfigurationSection
    {
        [System.Configuration.ConfigurationPropertyAttribute("Queue", IsRequired=true)]
        public string Queue { get; set; }
    }
    public class RijndaelEncryptionServiceConfig : System.Configuration.ConfigurationSection
    {
        [System.Configuration.ConfigurationPropertyAttribute("ExpiredKeys", IsRequired=false)]
        public NServiceBus.Config.RijndaelExpiredKeyCollection ExpiredKeys { get; set; }
        [System.Configuration.ConfigurationPropertyAttribute("Key", IsRequired=true)]
        public string Key { get; set; }
    }
    public class RijndaelExpiredKey : System.Configuration.ConfigurationElement, System.IComparable<NServiceBus.Config.RijndaelExpiredKey>
    {
        [System.Configuration.ConfigurationPropertyAttribute("Key", IsRequired=true)]
        public string Key { get; set; }
    }
    public class RijndaelExpiredKeyCollection : System.Configuration.ConfigurationElementCollection
    {
        public override System.Configuration.ConfigurationElementCollectionType CollectionType { get; }
        public NServiceBus.Config.RijndaelExpiredKey this[int index] { get; set; }
        public NServiceBus.Config.RijndaelExpiredKey this[string key] { get; }
        public void Add(NServiceBus.Config.RijndaelExpiredKey mapping) { }
        protected override void BaseAdd(System.Configuration.ConfigurationElement element) { }
        public void Clear() { }
        protected override System.Configuration.ConfigurationElement CreateNewElement() { }
        protected override System.Configuration.ConfigurationElement CreateNewElement(string elementName) { }
        protected override object GetElementKey(System.Configuration.ConfigurationElement element) { }
        public int IndexOf(NServiceBus.Config.RijndaelExpiredKey encryptionKey) { }
        public override bool IsReadOnly() { }
        public void Remove(NServiceBus.Config.RijndaelExpiredKey mapping) { }
        public void Remove(string name) { }
        public void RemoveAt(int index) { }
    }
    public class SecondLevelRetriesConfig : System.Configuration.ConfigurationSection
    {
        public SecondLevelRetriesConfig() { }
        public bool Enabled { get; set; }
        public int NumberOfRetries { get; set; }
        public System.TimeSpan TimeIncrease { get; set; }
    }
    public class TransportConfig : System.Configuration.ConfigurationSection
    {
        [System.Configuration.ConfigurationPropertyAttribute("MaximumConcurrencyLevel", DefaultValue=1, IsRequired=false)]
        public int MaximumConcurrencyLevel { get; set; }
        [System.Configuration.ConfigurationPropertyAttribute("MaximumMessageThroughputPerSecond", DefaultValue=0, IsRequired=false)]
        public int MaximumMessageThroughputPerSecond { get; set; }
        [System.Configuration.ConfigurationPropertyAttribute("MaxRetries", DefaultValue=5, IsRequired=false)]
        public int MaxRetries { get; set; }
    }
    public class UnicastBusConfig : System.Configuration.ConfigurationSection
    {
        [System.Configuration.ConfigurationPropertyAttribute("DistributorControlAddress", IsRequired=false)]
        public string DistributorControlAddress { get; set; }
        [System.Configuration.ConfigurationPropertyAttribute("DistributorDataAddress", IsRequired=false)]
        public string DistributorDataAddress { get; set; }
        [System.Configuration.ConfigurationPropertyAttribute("ForwardReceivedMessagesTo", IsRequired=false)]
        public string ForwardReceivedMessagesTo { get; set; }
        [System.Configuration.ConfigurationPropertyAttribute("MessageEndpointMappings", IsRequired=false)]
        public NServiceBus.Config.MessageEndpointMappingCollection MessageEndpointMappings { get; set; }
        [System.Configuration.ConfigurationPropertyAttribute("TimeoutManagerAddress", IsRequired=false)]
        public string TimeoutManagerAddress { get; set; }
        [System.Configuration.ConfigurationPropertyAttribute("TimeToBeReceivedOnForwardedMessages", IsRequired=false)]
        public System.TimeSpan TimeToBeReceivedOnForwardedMessages { get; set; }
    }
}
namespace NServiceBus.Config.ConfigurationSource
{
    
    public class DefaultConfigurationSource : NServiceBus.Config.ConfigurationSource.IConfigurationSource { }
    public interface IConfigurationSource
    {
        T GetConfiguration<T>()
            where T :  class, new ();
    }
    public interface IProvideConfiguration<T>
    
    {
        T GetConfiguration();
    }
}
namespace NServiceBus.Configuration.AdvanceExtensibility
{
    
    public class static AdvanceExtensibilityExtensions
    {
        public static NServiceBus.Settings.SettingsHolder GetSettings(this NServiceBus.Configuration.AdvanceExtensibility.ExposeSettings config) { }
    }
    public abstract class ExposeSettings
    {
        protected ExposeSettings(NServiceBus.Settings.SettingsHolder settings) { }
    }
}
namespace NServiceBus.Container
{
    
    public class ContainerCustomizations
    {
        public NServiceBus.Settings.SettingsHolder Settings { get; }
    }
    public abstract class ContainerDefinition
    {
        public abstract NServiceBus.ObjectBuilder.Common.IContainer CreateContainer(NServiceBus.Settings.ReadOnlySettings settings);
    }
}
namespace NServiceBus.DataBus
{
    
    public abstract class DataBusDefinition
    {
        protected internal abstract System.Type ProvidedByFeature();
    }
    public class DataBusExtentions : NServiceBus.Configuration.AdvanceExtensibility.ExposeSettings
    {
        public DataBusExtentions(NServiceBus.Settings.SettingsHolder settings) { }
    }
    public class DataBusExtentions<T> : NServiceBus.DataBus.DataBusExtentions
        where T : NServiceBus.DataBus.DataBusDefinition
    {
        public DataBusExtentions(NServiceBus.Settings.SettingsHolder settings) { }
    }
    public interface IDataBus
    {
        System.IO.Stream Get(string key);
        string Put(System.IO.Stream stream, System.TimeSpan timeToBeReceived);
        void Start();
    }
    public interface IDataBusSerializer
    {
        object Deserialize(System.IO.Stream stream);
        void Serialize(object databusProperty, System.IO.Stream stream);
    }
}
namespace NServiceBus.Encryption
{
    
    public interface IEncryptionService
    {
        string Decrypt(NServiceBus.EncryptedValue encryptedValue);
        NServiceBus.EncryptedValue Encrypt(string value);
    }
}
namespace NServiceBus.Encryption.Rijndael
{
    
    [System.ObsoleteAttribute("The Rijndael encryption functionality was an internal implementation detail of NS" +
        "ervicebus as such it has been removed from the public API. Will be removed in ve" +
        "rsion 6.0.0.", true)]
    public class EncryptionService { }
}
namespace NServiceBus.Faults
{
    
    public class ErrorsNotifications : System.IDisposable
    {
        public ErrorsNotifications() { }
        public System.IObservable<NServiceBus.Faults.SecondLevelRetry> MessageHasBeenSentToSecondLevelRetries { get; }
        public System.IObservable<NServiceBus.Faults.FirstLevelRetry> MessageHasFailedAFirstLevelRetryAttempt { get; }
        public System.IObservable<NServiceBus.Faults.FailedMessage> MessageSentToErrorQueue { get; }
    }
    public struct FailedMessage
    {
        public FailedMessage(System.Collections.Generic.Dictionary<string, string> headers, byte[] body, System.Exception exception) { }
        public byte[] Body { get; }
        public System.Exception Exception { get; }
        public System.Collections.Generic.Dictionary<string, string> Headers { get; }
    }
    public class static FaultsHeaderKeys
    {
        public const string FailedQ = "NServiceBus.FailedQ";
    }
    public struct FirstLevelRetry
    {
        public FirstLevelRetry(System.Collections.Generic.Dictionary<string, string> headers, byte[] body, System.Exception exception, int retryAttempt) { }
        public byte[] Body { get; }
        public System.Exception Exception { get; }
        public System.Collections.Generic.Dictionary<string, string> Headers { get; }
        public int RetryAttempt { get; }
    }
    public interface IManageMessageFailures
    {
        void Init(NServiceBus.Address address);
        void ProcessingAlwaysFailsForMessage(NServiceBus.TransportMessage message, System.Exception e);
        void SerializationFailedForMessage(NServiceBus.TransportMessage message, System.Exception e);
    }
    public struct SecondLevelRetry
    {
        public SecondLevelRetry(System.Collections.Generic.Dictionary<string, string> headers, byte[] body, System.Exception exception, int retryAttempt) { }
        public byte[] Body { get; }
        public System.Exception Exception { get; }
        public System.Collections.Generic.Dictionary<string, string> Headers { get; }
        public int RetryAttempt { get; }
    }
}
namespace NServiceBus.Features
{
    
    public class Audit : NServiceBus.Features.Feature
    {
        protected internal override void Setup(NServiceBus.Features.FeatureConfigurationContext context) { }
    }
    public class AutoSubscribe : NServiceBus.Features.Feature
    {
        protected internal override void Setup(NServiceBus.Features.FeatureConfigurationContext context) { }
    }
    public class BinarySerialization : NServiceBus.Features.Feature
    {
        protected internal override void Setup(NServiceBus.Features.FeatureConfigurationContext context) { }
    }
    public class BsonSerialization : NServiceBus.Features.Feature
    {
        protected internal override void Setup(NServiceBus.Features.FeatureConfigurationContext context) { }
    }
    public class CriticalTimeMonitoring : NServiceBus.Features.Feature
    {
        protected internal override void Setup(NServiceBus.Features.FeatureConfigurationContext context) { }
    }
    public class DataBus : NServiceBus.Features.Feature
    {
        protected internal override void Setup(NServiceBus.Features.FeatureConfigurationContext context) { }
    }
    public class Encryptor : NServiceBus.Features.Feature
    {
        protected internal override void Setup(NServiceBus.Features.FeatureConfigurationContext context) { }
    }
    public abstract class Feature
    {
        protected Feature() { }
        public bool IsActive { get; }
        public bool IsEnabledByDefault { get; }
        public string Name { get; }
        public string Version { get; }
        protected void Defaults(System.Action<NServiceBus.Settings.SettingsHolder> settings) { }
        protected void DependsOn<T>()
            where T : NServiceBus.Features.Feature { }
        protected void DependsOn(string featureName) { }
        protected void DependsOnAtLeastOne(params System.Type[] features) { }
        protected void DependsOnAtLeastOne(params string[] featureNames) { }
        protected void EnableByDefault() { }
        protected void Prerequisite(System.Func<NServiceBus.Features.FeatureConfigurationContext, bool> condition, string description) { }
        protected void RegisterStartupTask<T>()
            where T : NServiceBus.Features.FeatureStartupTask { }
        protected internal abstract void Setup(NServiceBus.Features.FeatureConfigurationContext context);
        public override string ToString() { }
    }
    public class FeatureConfigurationContext
    {
        public NServiceBus.ObjectBuilder.IConfigureComponents Container { get; }
        public NServiceBus.Pipeline.PipelineSettings Pipeline { get; }
        public NServiceBus.Settings.ReadOnlySettings Settings { get; }
    }
    public class FeatureDiagnosticData
    {
        public bool Active { get; }
        public System.Collections.Generic.IList<System.Collections.Generic.List<string>> Dependencies { get; }
        public bool DependenciesAreMeet { get; set; }
        public bool EnabledByDefault { get; }
        public string Name { get; }
        public NServiceBus.Features.PrerequisiteStatus PrerequisiteStatus { get; }
        public System.Collections.Generic.IList<System.Type> StartupTasks { get; }
        public string Version { get; }
    }
    [System.ObsoleteAttribute(@"Use `configuration.EnableFeature<T>()` or `configuration.DisableFeature<T>()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
    public class FeatureSettings { }
    public class FeaturesReport
    {
        public System.Collections.Generic.IList<NServiceBus.Features.FeatureDiagnosticData> Features { get; }
    }
    public abstract class FeatureStartupTask
    {
        protected abstract void OnStart();
        protected virtual void OnStop() { }
    }
    public class ForwardReceivedMessages : NServiceBus.Features.Feature
    {
        protected internal override void Setup(NServiceBus.Features.FeatureConfigurationContext context) { }
    }
    public class InMemoryGatewayPersistence : NServiceBus.Features.Feature
    {
        protected internal override void Setup(NServiceBus.Features.FeatureConfigurationContext context) { }
    }
    public class InMemoryOutboxPersistence : NServiceBus.Features.Feature
    {
        protected internal override void Setup(NServiceBus.Features.FeatureConfigurationContext context) { }
    }
    public class InMemorySagaPersistence : NServiceBus.Features.Feature
    {
        protected internal override void Setup(NServiceBus.Features.FeatureConfigurationContext context) { }
    }
    public class InMemorySubscriptionPersistence : NServiceBus.Features.Feature
    {
        protected internal override void Setup(NServiceBus.Features.FeatureConfigurationContext context) { }
    }
    public class InMemoryTimeoutPersistence : NServiceBus.Features.Feature
    {
        protected internal override void Setup(NServiceBus.Features.FeatureConfigurationContext context) { }
    }
    public class InstallationSupport : NServiceBus.Features.Feature
    {
        protected internal override void Setup(NServiceBus.Features.FeatureConfigurationContext context) { }
    }
    public class JsonSerialization : NServiceBus.Features.Feature
    {
        protected internal override void Setup(NServiceBus.Features.FeatureConfigurationContext context) { }
    }
    public class MessageDrivenSubscriptions : NServiceBus.Features.Feature
    {
        protected internal override void Setup(NServiceBus.Features.FeatureConfigurationContext context) { }
    }
    public class MsmqSubscriptionPersistence : NServiceBus.Features.Feature
    {
        protected internal override void Setup(NServiceBus.Features.FeatureConfigurationContext context) { }
    }
    public class MsmqTransportConfigurator : NServiceBus.Transports.ConfigureTransport
    {
        protected override string ExampleConnectionStringForErrorMessage { get; }
        protected override bool RequiresConnectionString { get; }
        protected override void Configure(NServiceBus.Features.FeatureConfigurationContext context, string connectionString) { }
    }
    public class Outbox : NServiceBus.Features.Feature
    {
        protected internal override void Setup(NServiceBus.Features.FeatureConfigurationContext context) { }
    }
    public class PrerequisiteStatus
    {
        public bool IsSatisfied { get; }
        public System.Collections.Generic.List<string> Reasons { get; }
    }
    public class Sagas : NServiceBus.Features.Feature
    {
        protected internal override void Setup(NServiceBus.Features.FeatureConfigurationContext context) { }
    }
    public class Scheduler : NServiceBus.Features.Feature
    {
        protected internal override void Setup(NServiceBus.Features.FeatureConfigurationContext context) { }
    }
    public class SecondLevelRetries : NServiceBus.Features.Feature
    {
        protected internal override void Setup(NServiceBus.Features.FeatureConfigurationContext context) { }
    }
    public class static SerializationFeatureHelper
    {
        public static bool ShouldSerializationFeatureBeEnabled(this NServiceBus.Features.Feature serializationFeature, NServiceBus.Features.FeatureConfigurationContext context) { }
    }
    public class static SettingsExtentions
    {
        public static NServiceBus.Settings.SettingsHolder EnableFeatureByDefault<T>(this NServiceBus.Settings.SettingsHolder settings)
            where T : NServiceBus.Features.Feature { }
        public static NServiceBus.Settings.SettingsHolder EnableFeatureByDefault(this NServiceBus.Settings.SettingsHolder settings, System.Type featureType) { }
    }
    public class SLAMonitoring : NServiceBus.Features.Feature
    {
        protected internal override void Setup(NServiceBus.Features.FeatureConfigurationContext context) { }
    }
    [System.ObsoleteAttribute("Please use `NServiceBus.Features.StorageDrivenPublishing` instead. Will be remove" +
        "d in version 6.0.0.", true)]
    public class StorageDrivenPublisher { }
    public class StorageDrivenPublishing : NServiceBus.Features.Feature
    {
        protected internal override void Setup(NServiceBus.Features.FeatureConfigurationContext context) { }
    }
    public class TimeoutManager : NServiceBus.Features.Feature
    {
        protected internal override void Setup(NServiceBus.Features.FeatureConfigurationContext context) { }
    }
    public class TimeoutManagerBasedDeferral : NServiceBus.Features.Feature
    {
        protected internal override void Setup(NServiceBus.Features.FeatureConfigurationContext context) { }
    }
    public class XmlSerialization : NServiceBus.Features.Feature
    {
        protected internal override void Setup(NServiceBus.Features.FeatureConfigurationContext context) { }
    }
}
namespace NServiceBus.Gateway.Deduplication
{
    
    public interface IDeduplicateMessages
    {
        bool DeduplicateMessage(string clientId, System.DateTime timeReceived);
    }
}
namespace NServiceBus.Hosting
{
    
    [System.ObsoleteAttribute("This class was never intended to be exposed as part of the public API. Will be re" +
        "moved in version 6.0.0.", true)]
    public class GenericHost { }
    public class HostInformation
    {
        public HostInformation(System.Guid hostId, string displayName) { }
        public HostInformation(System.Guid hostId, string displayName, System.Collections.Generic.Dictionary<string, string> properties) { }
        public string DisplayName { get; }
        public System.Guid HostId { get; }
        public System.Collections.Generic.Dictionary<string, string> Properties { get; }
    }
    [System.ObsoleteAttribute("This class was never intended to be exposed as part of the public API. Will be re" +
        "moved in version 6.0.0.", true)]
    public class IHost { }
}
namespace NServiceBus.Hosting.Helpers
{
    
    public class AssemblyScanner
    {
        public AssemblyScanner() { }
        public AssemblyScanner(string baseDirectoryToScan) { }
        public System.Collections.Generic.List<System.Reflection.Assembly> MustReferenceAtLeastOneAssembly { get; }
        public bool ThrowExceptions { get; set; }
        public NServiceBus.Hosting.Helpers.AssemblyScannerResults GetScannableAssemblies() { }
    }
    public class AssemblyScannerResults
    {
        public AssemblyScannerResults() { }
        public System.Collections.Generic.List<System.Reflection.Assembly> Assemblies { get; }
        public bool ErrorsThrownDuringScanning { get; }
        public System.Collections.Generic.List<NServiceBus.Hosting.Helpers.SkippedFile> SkippedFiles { get; }
    }
    public class SkippedFile
    {
        public string FilePath { get; }
        public string SkipReason { get; }
    }
}
namespace NServiceBus.IdGeneration
{
    
    [System.ObsoleteAttribute("This class was never intended to be exposed as part of the public API. Will be re" +
        "moved in version 6.0.0.", true)]
    public class static CombGuid { }
}
namespace NServiceBus.Installation.Environments
{
    
    [System.ObsoleteAttribute("IEnvironment is no longer required instead use the non generic `INeedToInstallSom" +
        "ething` and use `configuration.EnableInstallers()`, where `configuration` is an " +
        "instance of type `BusConfiguration` to execute them. Will be removed in version " +
        "6.0.0.", true)]
    public class Windows { }
}
namespace NServiceBus.Installation
{
    
    [System.ObsoleteAttribute("`IEnvironment` is no longer required instead use the non generic `INeedToInstallS" +
        "omething` and use `configuration.EnableInstallers()`, where `configuration` is a" +
        "n instance of type `BusConfiguration` to execute them. Will be removed in versio" +
        "n 6.0.0.", true)]
    public interface IEnvironment { }
    public interface INeedToInstallSomething
    {
        void Install(string identity, NServiceBus.Configure config);
    }
    [System.ObsoleteAttribute("`IEnvironment` is no longer required instead use the non generic `INeedToInstallS" +
        "omething` and use `configuration.EnableInstallers()`, where `configuration` is a" +
        "n instance of type `BusConfiguration` to execute them. Will be removed in versio" +
        "n 6.0.0.", true)]
    public class INeedToInstallSomething<T> { }
}
namespace NServiceBus.Logging
{
    
    public class DefaultFactory : NServiceBus.Logging.LoggingFactoryDefinition
    {
        public DefaultFactory() { }
        public void Directory(string directory) { }
        protected internal override NServiceBus.Logging.ILoggerFactory GetLoggingFactory() { }
        public void Level(NServiceBus.Logging.LogLevel level) { }
    }
    public interface ILog
    {
        bool IsDebugEnabled { get; }
        bool IsErrorEnabled { get; }
        bool IsFatalEnabled { get; }
        bool IsInfoEnabled { get; }
        bool IsWarnEnabled { get; }
        void Debug(string message);
        void Debug(string message, System.Exception exception);
        void DebugFormat(string format, params object[] args);
        void Error(string message);
        void Error(string message, System.Exception exception);
        void ErrorFormat(string format, params object[] args);
        void Fatal(string message);
        void Fatal(string message, System.Exception exception);
        void FatalFormat(string format, params object[] args);
        void Info(string message);
        void Info(string message, System.Exception exception);
        void InfoFormat(string format, params object[] args);
        void Warn(string message);
        void Warn(string message, System.Exception exception);
        void WarnFormat(string format, params object[] args);
    }
    public interface ILoggerFactory
    {
        NServiceBus.Logging.ILog GetLogger(System.Type type);
        NServiceBus.Logging.ILog GetLogger(string name);
    }
    public abstract class LoggingFactoryDefinition
    {
        protected internal abstract NServiceBus.Logging.ILoggerFactory GetLoggingFactory();
    }
    [System.ObsoleteAttribute("Since the case where this exception was thrown should not be handled by consumers" +
        " of the API it has been removed. Will be removed in version 6.0.0.", true)]
    public class LoggingLibraryException : System.Exception { }
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warn = 2,
        Error = 3,
        Fatal = 4,
    }
    public class static LogManager
    {
        public static NServiceBus.Logging.ILog GetLogger<T>() { }
        public static NServiceBus.Logging.ILog GetLogger(System.Type type) { }
        public static NServiceBus.Logging.ILog GetLogger(string name) { }
        public static T Use<T>()
            where T : NServiceBus.Logging.LoggingFactoryDefinition, new () { }
        public static void UseFactory(NServiceBus.Logging.ILoggerFactory loggerFactory) { }
    }
}
namespace NServiceBus.Logging.Log4NetBridge
{
    
    [System.ObsoleteAttribute("Sensible defaults for logging are now built into NServicebus. To customise loggin" +
        "g there are external nuget packages available to connect NServiceBus to the vari" +
        "ous popular logging frameworks. Will be removed in version 6.0.0.", true)]
    public class ConfigureInternalLog4NetBridge { }
}
namespace NServiceBus.Logging.Loggers
{
    
    [System.ObsoleteAttribute("Sensible defaults for logging are now built into NServicebus. To customise loggin" +
        "g there are external nuget packages available to connect NServiceBus to the vari" +
        "ous popular logging frameworks. Will be removed in version 6.0.0.", true)]
    public class ConsoleLogger { }
    [System.ObsoleteAttribute("Sensible defaults for logging are now built into NServicebus. To customise loggin" +
        "g there are external nuget packages available to connect NServiceBus to the vari" +
        "ous popular logging frameworks. Will be removed in version 6.0.0.", true)]
    public class ConsoleLoggerFactory { }
    [System.ObsoleteAttribute("Sensible defaults for logging are now built into NServicebus. To customise loggin" +
        "g there are external nuget packages available to connect NServiceBus to the vari" +
        "ous popular logging frameworks. Will be removed in version 6.0.0.", true)]
    public class NullLogger { }
    [System.ObsoleteAttribute("Sensible defaults for logging are now built into NServicebus. To customise loggin" +
        "g there are external nuget packages available to connect NServiceBus to the vari" +
        "ous popular logging frameworks. Will be removed in version 6.0.0.", true)]
    public class NullLoggerFactory { }
}
namespace NServiceBus.Logging.Loggers.Log4NetAdapter
{
    
    [System.ObsoleteAttribute("Sensible defaults for logging are now built into NServicebus. To customise loggin" +
        "g there are external nuget packages available to connect NServiceBus to the vari" +
        "ous popular logging frameworks. Will be removed in version 6.0.0.", true)]
    public class Log4NetAppenderFactory { }
    [System.ObsoleteAttribute("Sensible defaults for logging are now built into NServicebus. To customise loggin" +
        "g there are external nuget packages available to connect NServiceBus to the vari" +
        "ous popular logging frameworks. Will be removed in version 6.0.0.", true)]
    public class Log4NetConfigurator { }
    [System.ObsoleteAttribute("Sensible defaults for logging are now built into NServicebus. To customise loggin" +
        "g there are external nuget packages available to connect NServiceBus to the vari" +
        "ous popular logging frameworks. Will be removed in version 6.0.0.", true)]
    public class Log4NetLogger { }
    [System.ObsoleteAttribute("Sensible defaults for logging are now built into NServicebus. To customise loggin" +
        "g there are external nuget packages available to connect NServiceBus to the vari" +
        "ous popular logging frameworks. Will be removed in version 6.0.0.", true)]
    public class Log4NetLoggerFactory { }
}
namespace NServiceBus.Logging.Loggers.NLogAdapter
{
    
    [System.ObsoleteAttribute("Sensible defaults for logging are now built into NServicebus. To customise loggin" +
        "g there are external nuget packages available to connect NServiceBus to the vari" +
        "ous popular logging frameworks. Will be removed in version 6.0.0.", true)]
    public class NLogConfigurator { }
    [System.ObsoleteAttribute("Sensible defaults for logging are now built into NServicebus. To customise loggin" +
        "g there are external nuget packages available to connect NServiceBus to the vari" +
        "ous popular logging frameworks. Will be removed in version 6.0.0.", true)]
    public class NLogLogger { }
    [System.ObsoleteAttribute("Sensible defaults for logging are now built into NServicebus. To customise loggin" +
        "g there are external nuget packages available to connect NServiceBus to the vari" +
        "ous popular logging frameworks. Will be removed in version 6.0.0.", true)]
    public class NLogLoggerFactory { }
    [System.ObsoleteAttribute("Sensible defaults for logging are now built into NServicebus. To customise loggin" +
        "g there are external nuget packages available to connect NServiceBus to the vari" +
        "ous popular logging frameworks. Will be removed in version 6.0.0.", true)]
    public class NLogTargetFactory { }
}
namespace NServiceBus.MessageInterfaces
{
    
    public interface IMessageMapper : NServiceBus.IMessageCreator
    {
        System.Type GetMappedTypeFor(System.Type t);
        System.Type GetMappedTypeFor(string typeName);
        void Initialize(System.Collections.Generic.IEnumerable<System.Type> types);
    }
}
namespace NServiceBus.MessageInterfaces.MessageMapper.Reflection
{
    
    public class MessageMapper : NServiceBus.IMessageCreator, NServiceBus.MessageInterfaces.IMessageMapper
    {
        public MessageMapper() { }
        public T CreateInstance<T>(System.Action<T> action) { }
        public T CreateInstance<T>() { }
        public object CreateInstance(System.Type t) { }
        public System.Type GetMappedTypeFor(System.Type t) { }
        public System.Type GetMappedTypeFor(string typeName) { }
        public void Initialize(System.Collections.Generic.IEnumerable<System.Type> types) { }
    }
}
namespace NServiceBus.MessageMutator
{
    
    public interface IMessageMutator : NServiceBus.MessageMutator.IMutateIncomingMessages, NServiceBus.MessageMutator.IMutateOutgoingMessages { }
    public interface IMutateIncomingMessages
    {
        object MutateIncoming(object message);
    }
    public interface IMutateIncomingTransportMessages
    {
        void MutateIncoming(NServiceBus.TransportMessage transportMessage);
    }
    public interface IMutateOutgoingMessages
    {
        object MutateOutgoing(object message);
    }
    public interface IMutateOutgoingTransportMessages
    {
        void MutateOutgoing(NServiceBus.Unicast.Messages.LogicalMessage logicalMessage, NServiceBus.TransportMessage transportMessage);
    }
    public interface IMutateTransportMessages : NServiceBus.MessageMutator.IMutateIncomingTransportMessages, NServiceBus.MessageMutator.IMutateOutgoingTransportMessages { }
}
namespace NServiceBus.ObjectBuilder.Common.Config
{
    
    [System.ObsoleteAttribute("Use `configuration.UseContainer<T>()`, where configuration is an instance of type" +
        " `BusConfiguration`. Will be removed in version 6.0.0.", true)]
    public class static ConfigureContainer
    {
        [System.ObsoleteAttribute("Use `configuration.UseContainer<T>()`, where configuration is an instance of type" +
            " `BusConfiguration`. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure UsingContainer<T>(this NServiceBus.Configure configure)
            where T :  class, NServiceBus.ObjectBuilder.Common.IContainer, new () { }
        [System.ObsoleteAttribute("Use `configuration.UseContainer(container)`, where configuration is an instance o" +
            "f type `BusConfiguration`. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Configure UsingContainer<T>(this NServiceBus.Configure configure, T container)
            where T : NServiceBus.ObjectBuilder.Common.IContainer { }
    }
}
namespace NServiceBus.ObjectBuilder.Common
{
    
    public interface IContainer : System.IDisposable
    {
        object Build(System.Type typeToBuild);
        System.Collections.Generic.IEnumerable<object> BuildAll(System.Type typeToBuild);
        NServiceBus.ObjectBuilder.Common.IContainer BuildChildContainer();
        void Configure(System.Type component, NServiceBus.DependencyLifecycle dependencyLifecycle);
        void Configure<T>(System.Func<T> component, NServiceBus.DependencyLifecycle dependencyLifecycle);
        void ConfigureProperty(System.Type component, string property, object value);
        bool HasComponent(System.Type componentType);
        void RegisterSingleton(System.Type lookupType, object instance);
        void Release(object instance);
    }
}
namespace NServiceBus.ObjectBuilder
{
    
    public interface IBuilder : System.IDisposable
    {
        object Build(System.Type typeToBuild);
        T Build<T>();
        System.Collections.Generic.IEnumerable<T> BuildAll<T>();
        System.Collections.Generic.IEnumerable<object> BuildAll(System.Type typeToBuild);
        void BuildAndDispatch(System.Type typeToBuild, System.Action<object> action);
        NServiceBus.ObjectBuilder.IBuilder CreateChildBuilder();
        void Release(object instance);
    }
    public interface IComponentConfig
    {
        NServiceBus.ObjectBuilder.IComponentConfig ConfigureProperty(string name, object value);
    }
    public interface IComponentConfig<T>
    
    {
        NServiceBus.ObjectBuilder.IComponentConfig<T> ConfigureProperty(System.Linq.Expressions.Expression<System.Func<T, object>> property, object value);
    }
    public interface IConfigureComponents
    {
        NServiceBus.ObjectBuilder.IComponentConfig ConfigureComponent(System.Type concreteComponent, NServiceBus.DependencyLifecycle dependencyLifecycle);
        NServiceBus.ObjectBuilder.IComponentConfig<T> ConfigureComponent<T>(NServiceBus.DependencyLifecycle dependencyLifecycle);
        NServiceBus.ObjectBuilder.IComponentConfig<T> ConfigureComponent<T>(System.Func<T> componentFactory, NServiceBus.DependencyLifecycle dependencyLifecycle);
        NServiceBus.ObjectBuilder.IComponentConfig<T> ConfigureComponent<T>(System.Func<NServiceBus.ObjectBuilder.IBuilder, T> componentFactory, NServiceBus.DependencyLifecycle dependencyLifecycle);
        NServiceBus.ObjectBuilder.IConfigureComponents ConfigureProperty<T>(System.Linq.Expressions.Expression<System.Func<T, object>> property, object value);
        NServiceBus.ObjectBuilder.IConfigureComponents ConfigureProperty<T>(string propertyName, object value);
        bool HasComponent<T>();
        bool HasComponent(System.Type componentType);
        NServiceBus.ObjectBuilder.IConfigureComponents RegisterSingleton(System.Type lookupType, object instance);
        NServiceBus.ObjectBuilder.IConfigureComponents RegisterSingleton<T>(T instance);
    }
}
namespace NServiceBus.Outbox
{
    
    public interface IOutboxStorage
    {
        void SetAsDispatched(string messageId);
        void Store(string messageId, System.Collections.Generic.IEnumerable<NServiceBus.Outbox.TransportOperation> transportOperations);
        bool TryGet(string messageId, out NServiceBus.Outbox.OutboxMessage message);
    }
    public class OutboxMessage
    {
        public OutboxMessage(string messageId) { }
        public string MessageId { get; }
        public System.Collections.Generic.List<NServiceBus.Outbox.TransportOperation> TransportOperations { get; }
    }
    public class OutboxSettings
    {
        public void TimeToKeepDeduplicationData(System.TimeSpan time) { }
    }
    public class TransportOperation
    {
        public TransportOperation(string messageId, System.Collections.Generic.Dictionary<string, string> options, byte[] body, System.Collections.Generic.Dictionary<string, string> headers) { }
        public byte[] Body { get; }
        public System.Collections.Generic.Dictionary<string, string> Headers { get; }
        public string MessageId { get; }
        public System.Collections.Generic.Dictionary<string, string> Options { get; }
    }
}
namespace NServiceBus.Persistence
{
    
    [System.ObsoleteAttribute("Since the case where this exception was thrown should not be handled by consumers" +
        " of the API it has been removed. Will be removed in version 6.0.0.", true)]
    public class ConcurrencyException : System.Exception { }
    public abstract class PersistenceDefinition
    {
        protected PersistenceDefinition() { }
        protected void Defaults(System.Action<NServiceBus.Settings.SettingsHolder> action) { }
        [System.ObsoleteAttribute("Please use `HasSupportFor<T>()` instead. Will be treated as an error from version" +
            " 6.0.0. Will be removed in version 7.0.0.", false)]
        public bool HasSupportFor(NServiceBus.Persistence.Storage storage) { }
        public bool HasSupportFor<T>()
            where T : NServiceBus.Persistence.StorageType { }
        protected void Supports<T>(System.Action<NServiceBus.Settings.SettingsHolder> action)
            where T : NServiceBus.Persistence.StorageType { }
        [System.ObsoleteAttribute("Please use `Supports<T>()` instead. Will be treated as an error from version 6.0." +
            "0. Will be removed in version 7.0.0.", false)]
        protected void Supports(NServiceBus.Persistence.Storage storage, System.Action<NServiceBus.Settings.SettingsHolder> action) { }
    }
    [System.ObsoleteAttribute("Please use `NServiceBus.Persistence.StorageType` instead. Will be treated as an e" +
        "rror from version 6.0.0. Will be removed in version 7.0.0.", false)]
    public enum Storage
    {
        Timeouts = 1,
        Subscriptions = 2,
        Sagas = 3,
        GatewayDeduplication = 4,
        Outbox = 5,
    }
    public abstract class StorageType
    {
        public override string ToString() { }
        public sealed class GatewayDeduplication : NServiceBus.Persistence.StorageType { }
        public sealed class Outbox : NServiceBus.Persistence.StorageType { }
        public sealed class Sagas : NServiceBus.Persistence.StorageType { }
        public sealed class Subscriptions : NServiceBus.Persistence.StorageType { }
        public sealed class Timeouts : NServiceBus.Persistence.StorageType { }
    }
}
namespace NServiceBus.Persistence.Legacy
{
    
    public class MsmqPersistence : NServiceBus.Persistence.PersistenceDefinition { }
}
namespace NServiceBus.Pipeline
{
    
    public abstract class BehaviorContext
    {
        protected readonly NServiceBus.Pipeline.BehaviorContext parentContext;
        protected BehaviorContext(NServiceBus.Pipeline.BehaviorContext parentContext) { }
        public NServiceBus.ObjectBuilder.IBuilder Builder { get; }
        public T Get<T>() { }
        public T Get<T>(string key) { }
        public void Remove<T>() { }
        public void Remove(string key) { }
        public void Set<T>(T t) { }
        public void Set<T>(string key, T t) { }
        public bool TryGet<T>(out T result) { }
        public bool TryGet<T>(string key, out T result) { }
    }
    public interface IBehavior<in TContext>
        where in TContext : NServiceBus.Pipeline.BehaviorContext
    {
        void Invoke(TContext context, System.Action next);
    }
    public class PipelineExecutor : System.IDisposable
    {
        public PipelineExecutor(NServiceBus.Settings.ReadOnlySettings settings, NServiceBus.ObjectBuilder.IBuilder builder, NServiceBus.BusNotifications busNotifications) { }
        public NServiceBus.Pipeline.BehaviorContext CurrentContext { get; }
        public System.Collections.Generic.IList<NServiceBus.Pipeline.RegisterStep> Incoming { get; }
        public System.Collections.Generic.IList<NServiceBus.Pipeline.RegisterStep> Outgoing { get; }
        public void Dispose() { }
        public void InvokePipeline<TContext>(System.Collections.Generic.IEnumerable<System.Type> behaviors, TContext context)
            where TContext : NServiceBus.Pipeline.BehaviorContext { }
    }
    public class PipelineNotifications : System.IDisposable
    {
        public PipelineNotifications() { }
        public System.IObservable<System.IObservable<NServiceBus.Pipeline.StepStarted>> ReceiveStarted { get; }
    }
    public class PipelineSettings
    {
        public PipelineSettings(NServiceBus.BusConfiguration config) { }
        public void Register(string stepId, System.Type behavior, string description) { }
        public void Register(NServiceBus.Pipeline.WellKnownStep wellKnownStep, System.Type behavior, string description) { }
        public void Register<T>()
            where T : NServiceBus.Pipeline.RegisterStep, new () { }
        public void Remove(string stepId) { }
        public void Remove(NServiceBus.Pipeline.WellKnownStep wellKnownStep) { }
        public void Replace(string stepId, System.Type newBehavior, string description = null) { }
        public void Replace(NServiceBus.Pipeline.WellKnownStep wellKnownStep, System.Type newBehavior, string description = null) { }
    }
    public abstract class RegisterStep
    {
        protected RegisterStep(string stepId, System.Type behavior, string description) { }
        public System.Type BehaviorType { get; }
        public string Description { get; }
        public string StepId { get; }
        public void InsertAfter(NServiceBus.Pipeline.WellKnownStep step) { }
        public void InsertAfter(string id) { }
        public void InsertAfterIfExists(NServiceBus.Pipeline.WellKnownStep step) { }
        public void InsertAfterIfExists(string id) { }
        public void InsertBefore(NServiceBus.Pipeline.WellKnownStep step) { }
        public void InsertBefore(string id) { }
        public void InsertBeforeIfExists(NServiceBus.Pipeline.WellKnownStep step) { }
        public void InsertBeforeIfExists(string id) { }
    }
    public struct StepEnded
    {
        public StepEnded(System.TimeSpan duration) { }
        public System.TimeSpan Duration { get; }
    }
    public struct StepStarted
    {
        public StepStarted(string stepId, System.Type behavior, System.IObservable<NServiceBus.Pipeline.StepEnded> stepEnded) { }
        public System.Type Behavior { get; }
        public System.IObservable<NServiceBus.Pipeline.StepEnded> Ended { get; }
        public string StepId { get; }
    }
    public class WellKnownStep
    {
        public static readonly NServiceBus.Pipeline.WellKnownStep AuditProcessedMessage;
        public static readonly NServiceBus.Pipeline.WellKnownStep CreateChildContainer;
        public static readonly NServiceBus.Pipeline.WellKnownStep CreatePhysicalMessage;
        public static readonly NServiceBus.Pipeline.WellKnownStep DeserializeMessages;
        public static readonly NServiceBus.Pipeline.WellKnownStep DispatchMessageToTransport;
        public static readonly NServiceBus.Pipeline.WellKnownStep EnforceBestPractices;
        public static readonly NServiceBus.Pipeline.WellKnownStep ExecuteLogicalMessages;
        public static readonly NServiceBus.Pipeline.WellKnownStep ExecuteUnitOfWork;
        public static readonly NServiceBus.Pipeline.WellKnownStep InvokeHandlers;
        public static readonly NServiceBus.Pipeline.WellKnownStep InvokeSaga;
        public static readonly NServiceBus.Pipeline.WellKnownStep LoadHandlers;
        public static readonly NServiceBus.Pipeline.WellKnownStep MutateIncomingMessages;
        public static readonly NServiceBus.Pipeline.WellKnownStep MutateIncomingTransportMessage;
        public static readonly NServiceBus.Pipeline.WellKnownStep MutateOutgoingMessages;
        public static readonly NServiceBus.Pipeline.WellKnownStep MutateOutgoingTransportMessage;
        public static NServiceBus.Pipeline.WellKnownStep ProcessingStatistics;
        public static readonly NServiceBus.Pipeline.WellKnownStep SerializeMessage;
    }
}
namespace NServiceBus.Pipeline.Contexts
{
    
    public class IncomingContext : NServiceBus.Pipeline.BehaviorContext
    {
        public IncomingContext(NServiceBus.Pipeline.BehaviorContext parentContext, NServiceBus.TransportMessage transportMessage) { }
        public bool HandlerInvocationAborted { get; }
        public NServiceBus.Unicast.Messages.LogicalMessage IncomingLogicalMessage { get; set; }
        public System.Collections.Generic.List<NServiceBus.Unicast.Messages.LogicalMessage> LogicalMessages { get; set; }
        public NServiceBus.Unicast.Behaviors.MessageHandler MessageHandler { get; set; }
        public NServiceBus.TransportMessage PhysicalMessage { get; }
        public void DoNotInvokeAnyMoreHandlers() { }
    }
    public class OutgoingContext : NServiceBus.Pipeline.BehaviorContext
    {
        public OutgoingContext(NServiceBus.Pipeline.BehaviorContext parentContext, NServiceBus.Unicast.DeliveryOptions deliveryOptions, NServiceBus.Unicast.Messages.LogicalMessage message) { }
        public NServiceBus.Unicast.DeliveryOptions DeliveryOptions { get; }
        public NServiceBus.TransportMessage IncomingMessage { get; }
        public NServiceBus.Unicast.Messages.LogicalMessage OutgoingLogicalMessage { get; }
        public NServiceBus.TransportMessage OutgoingMessage { get; }
    }
}
namespace NServiceBus.Saga
{
    
    public abstract class ContainSagaData : NServiceBus.Saga.IContainSagaData
    {
        public virtual System.Guid Id { get; set; }
        public virtual string OriginalMessageId { get; set; }
        public virtual string Originator { get; set; }
    }
    [System.ObsoleteAttribute("Since `ISaga` has been merged into the abstract class `Saga` this interface is no" +
        " longer required. Please use `NServiceBus.Saga.Saga.Completed` instead. Will be " +
        "removed in version 6.0.0.", true)]
    public interface HasCompleted { }
    public interface IAmStartedByMessages<T> : NServiceBus.IHandleMessages<T> { }
    [System.ObsoleteAttribute("Since `ISaga` has been merged into the abstract class `Saga` this interface is no" +
        " longer required. Please use `NServiceBus.Saga.Saga` instead. Will be removed in" +
        " version 6.0.0.", true)]
    public interface IConfigurable { }
    public interface IConfigureHowToFindSagaWithMessage
    {
        void ConfigureMapping<TSagaEntity, TMessage>(System.Linq.Expressions.Expression<System.Func<TSagaEntity, object>> sagaEntityProperty, System.Linq.Expressions.Expression<System.Func<TMessage, object>> messageProperty)
            where TSagaEntity : NServiceBus.Saga.IContainSagaData
        ;
    }
    public interface IContainSagaData
    {
        System.Guid Id { get; set; }
        string OriginalMessageId { get; set; }
        string Originator { get; set; }
    }
    public interface IFinder { }
    public abstract class IFindSagas<T>
        where T : NServiceBus.Saga.IContainSagaData
    {
        public interface Using<T, M> : NServiceBus.Saga.IFinder
            where T : NServiceBus.Saga.IContainSagaData
        
        {
            T FindBy(M message);
        }
    }
    public interface IHandleSagaNotFound
    {
        void Handle(object message);
    }
    public interface IHandleTimeouts<T>
    
    {
        void Timeout(T state);
    }
    [System.ObsoleteAttribute("Please use `ISagaPersister` instead. Will be removed in version 6.0.0.", true)]
    public interface IPersistSagas { }
    [System.ObsoleteAttribute("Please use `NServiceBus.Saga.Saga` instead. Will be removed in version 6.0.0.", true)]
    public interface ISaga { }
    [System.ObsoleteAttribute("Please use `NServiceBus.Saga.Saga<T>` instead. Will be removed in version 6.0.0.", true)]
    public interface ISaga<T> { }
    public interface ISagaPersister
    {
        void Complete(NServiceBus.Saga.IContainSagaData saga);
        TSagaData Get<TSagaData>(System.Guid sagaId)
            where TSagaData : NServiceBus.Saga.IContainSagaData;
        TSagaData Get<TSagaData>(string propertyName, object propertyValue)
            where TSagaData : NServiceBus.Saga.IContainSagaData;
        void Save(NServiceBus.Saga.IContainSagaData saga);
        void Update(NServiceBus.Saga.IContainSagaData saga);
    }
    public abstract class Saga
    {
        public NServiceBus.IBus Bus { get; set; }
        public bool Completed { get; }
        public NServiceBus.Saga.IContainSagaData Entity { get; set; }
        protected internal abstract void ConfigureHowToFindSaga(NServiceBus.Saga.IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration);
        protected virtual void MarkAsComplete() { }
        protected virtual void ReplyToOriginator(object message) { }
        [System.ObsoleteAttribute("Construct your message and pass it to the non Action overload. Please use `Saga.R" +
            "eplyToOriginator(object)` instead. Will be treated as an error from version 6.0." +
            "0. Will be removed in version 6.0.0.", false)]
        protected virtual void ReplyToOriginator<TMessage>(System.Action<TMessage> messageConstructor)
            where TMessage : new() { }
        protected void RequestTimeout<TTimeoutMessageType>(System.DateTime at)
            where TTimeoutMessageType : new() { }
        [System.ObsoleteAttribute("Construct your message and pass it to the non Action overload. Please use `Saga.R" +
            "equestTimeout<TTimeoutMessageType>(DateTime, TTimeoutMessageType)` instead. Will" +
            " be treated as an error from version 6.0.0. Will be removed in version 6.0.0.", false)]
        protected void RequestTimeout<TTimeoutMessageType>(System.DateTime at, System.Action<TTimeoutMessageType> action)
            where TTimeoutMessageType : new() { }
        protected void RequestTimeout<TTimeoutMessageType>(System.DateTime at, TTimeoutMessageType timeoutMessage) { }
        protected void RequestTimeout<TTimeoutMessageType>(System.TimeSpan within)
            where TTimeoutMessageType : new() { }
        [System.ObsoleteAttribute("Construct your message and pass it to the non Action overload. Please use `Saga.R" +
            "equestTimeout<TTimeoutMessageType>(TimeSpan, TTimeoutMessageType)` instead. Will" +
            " be treated as an error from version 6.0.0. Will be removed in version 6.0.0.", false)]
        protected void RequestTimeout<TTimeoutMessageType>(System.TimeSpan within, System.Action<TTimeoutMessageType> messageConstructor)
            where TTimeoutMessageType : new() { }
        protected void RequestTimeout<TTimeoutMessageType>(System.TimeSpan within, TTimeoutMessageType timeoutMessage) { }
    }
    public abstract class Saga<TSagaData> : NServiceBus.Saga.Saga
        where TSagaData : NServiceBus.Saga.IContainSagaData, new ()
    {
        public TSagaData Data { get; set; }
        protected internal override void ConfigureHowToFindSaga(NServiceBus.Saga.IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration) { }
        protected abstract void ConfigureHowToFindSaga(NServiceBus.Saga.SagaPropertyMapper<TSagaData> mapper);
    }
    public class SagaPropertyMapper<TSagaData>
        where TSagaData : NServiceBus.Saga.IContainSagaData
    {
        public NServiceBus.Saga.ToSagaExpression<TSagaData, TMessage> ConfigureMapping<TMessage>(System.Linq.Expressions.Expression<System.Func<TMessage, object>> messageProperty) { }
    }
    public class ToSagaExpression<TSagaData, TMessage>
        where TSagaData : NServiceBus.Saga.IContainSagaData
    
    {
        public ToSagaExpression(NServiceBus.Saga.IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration, System.Linq.Expressions.Expression<System.Func<TMessage, object>> messageProperty) { }
        public void ToSaga(System.Linq.Expressions.Expression<System.Func<TSagaData, object>> sagaEntityProperty) { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Property | System.AttributeTargets.All)]
    public sealed class UniqueAttribute : System.Attribute
    {
        public static System.Collections.Generic.IDictionary<string, object> GetUniqueProperties(NServiceBus.Saga.IContainSagaData entity) { }
        public static System.Collections.Generic.IEnumerable<System.Reflection.PropertyInfo> GetUniqueProperties(System.Type type) { }
        public static System.Reflection.PropertyInfo GetUniqueProperty(System.Type type) { }
        public static System.Nullable<System.Collections.Generic.KeyValuePair<string, object>> GetUniqueProperty(NServiceBus.Saga.IContainSagaData entity) { }
    }
}
namespace NServiceBus.Sagas
{
    
    public class ActiveSagaInstance
    {
        [System.ObsoleteAttribute("Please use `context.MessageHandler.Instance` instead. Will be treated as an error" +
            " from version 6.0.0. Will be removed in version 7.0.0.", false)]
        public NServiceBus.Saga.Saga Instance { get; }
        public bool IsNew { get; }
        public bool NotFound { get; }
        public string SagaId { get; }
        [System.ObsoleteAttribute("Please use `.Metadata.SagaType` instead. Will be treated as an error from version" +
            " 6.0.0. Will be removed in version 7.0.0.", false)]
        public System.Type SagaType { get; }
        public void AttachNewEntity(NServiceBus.Saga.IContainSagaData sagaEntity) { }
    }
}
namespace NServiceBus.Satellites
{
    
    public interface IAdvancedSatellite : NServiceBus.Satellites.ISatellite
    {
        System.Action<NServiceBus.Unicast.Transport.TransportReceiver> GetReceiverCustomization();
    }
    public interface ISatellite
    {
        bool Disabled { get; }
        NServiceBus.Address InputAddress { get; }
        bool Handle(NServiceBus.TransportMessage message);
        void Start();
        void Stop();
    }
}
namespace NServiceBus.SecondLevelRetries.Config
{
    
    public class SecondLevelRetriesSettings
    {
        public void CustomRetryPolicy(System.Func<NServiceBus.TransportMessage, System.TimeSpan> customPolicy) { }
    }
}
namespace NServiceBus.SecondLevelRetries.Helpers
{
    
    [System.ObsoleteAttribute("Access the `TransportMessage.Headers` dictionary directly. Will be removed in ver" +
        "sion 6.0.0.", true)]
    public class static TransportMessageHelpers
    {
        [System.ObsoleteAttribute("Access the `TransportMessage.Headers` dictionary directly using the `FaultsHeader" +
            "Keys.FailedQ` key. Will be removed in version 6.0.0.", true)]
        public static NServiceBus.Address GetAddressOfFaultingEndpoint(NServiceBus.TransportMessage message) { }
        [System.ObsoleteAttribute("Access the `TransportMessage.Headers` dictionary directly. Will be removed in ver" +
            "sion 6.0.0.", true)]
        public static string GetHeader(NServiceBus.TransportMessage message, string key) { }
        [System.ObsoleteAttribute("Access the `TransportMessage.Headers` dictionary directly using the `Headers.Retr" +
            "ies` key. Will be removed in version 6.0.0.", true)]
        public static int GetNumberOfRetries(NServiceBus.TransportMessage message) { }
        [System.ObsoleteAttribute("Access the `TransportMessage.Headers` dictionary directly. Will be removed in ver" +
            "sion 6.0.0.", true)]
        public static bool HeaderExists(NServiceBus.TransportMessage message, string key) { }
        [System.ObsoleteAttribute("Access the `TransportMessage.Headers` dictionary directly. Will be removed in ver" +
            "sion 6.0.0.", true)]
        public static void SetHeader(NServiceBus.TransportMessage message, string key, string value) { }
    }
}
namespace NServiceBus.Serialization
{
    
    public interface IMessageSerializer
    {
        string ContentType { get; }
        object[] Deserialize(System.IO.Stream stream, System.Collections.Generic.IList<System.Type> messageTypes = null);
        void Serialize(object message, System.IO.Stream stream);
    }
    public abstract class SerializationDefinition
    {
        protected internal abstract System.Type ProvidedByFeature();
    }
    public class SerializationExtentions<T> : NServiceBus.Configuration.AdvanceExtensibility.ExposeSettings
        where T : NServiceBus.Serialization.SerializationDefinition
    {
        public SerializationExtentions(NServiceBus.Settings.SettingsHolder settings) { }
    }
}
namespace NServiceBus.Serializers.Binary
{
    
    public class BinaryMessageSerializer : NServiceBus.Serialization.IMessageSerializer
    {
        public BinaryMessageSerializer() { }
        public string ContentType { get; }
        public object[] Deserialize(System.IO.Stream stream, System.Collections.Generic.IList<System.Type> messageTypes = null) { }
        public void Serialize(object message, System.IO.Stream stream) { }
    }
    public class SimpleMessageMapper : NServiceBus.IMessageCreator, NServiceBus.MessageInterfaces.IMessageMapper { }
}
namespace NServiceBus.Serializers.Json
{
    
    public class BsonMessageSerializer : NServiceBus.Serializers.Json.JsonMessageSerializerBase
    {
        public BsonMessageSerializer(NServiceBus.MessageInterfaces.IMessageMapper messageMapper) { }
        protected internal override Newtonsoft.Json.JsonReader CreateJsonReader(System.IO.Stream stream) { }
        protected internal override Newtonsoft.Json.JsonWriter CreateJsonWriter(System.IO.Stream stream) { }
        protected internal override string GetContentType() { }
    }
    public class JsonMessageSerializer : NServiceBus.Serializers.Json.JsonMessageSerializerBase
    {
        public JsonMessageSerializer(NServiceBus.MessageInterfaces.IMessageMapper messageMapper) { }
        public System.Text.Encoding Encoding { get; set; }
        protected internal override Newtonsoft.Json.JsonReader CreateJsonReader(System.IO.Stream stream) { }
        protected internal override Newtonsoft.Json.JsonWriter CreateJsonWriter(System.IO.Stream stream) { }
        public object DeserializeObject(string value, System.Type type) { }
        protected internal override string GetContentType() { }
        public string SerializeObject(object value) { }
    }
    public abstract class JsonMessageSerializerBase : NServiceBus.Serialization.IMessageSerializer
    {
        protected internal JsonMessageSerializerBase(NServiceBus.MessageInterfaces.IMessageMapper messageMapper) { }
        public string ContentType { get; }
        [System.ObsoleteAttribute("In version 5 multi-message sends was removed. So Wrapping messages is no longer r" +
            "equired. If you are communicating with version 3 ensure you are on the latets 3." +
            "3.x. Will be removed in version 6.0.0.", true)]
        public bool SkipArrayWrappingForSingleMessages { get; set; }
        protected internal abstract Newtonsoft.Json.JsonReader CreateJsonReader(System.IO.Stream stream);
        protected internal abstract Newtonsoft.Json.JsonWriter CreateJsonWriter(System.IO.Stream stream);
        public object[] Deserialize(System.IO.Stream stream, System.Collections.Generic.IList<System.Type> messageTypes) { }
        protected internal abstract string GetContentType();
        public void Serialize(object message, System.IO.Stream stream) { }
    }
}
namespace NServiceBus.Serializers.XML.Config
{
    
    [System.ObsoleteAttribute(@"Use configuration.UseSerialization<XmlSerializer>(), where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
    public class XmlSerializationSettings
    {
        [System.ObsoleteAttribute(@"Use `configuration.UseSerialization<XmlSerializer>().DontWrapRawXml()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public NServiceBus.Serializers.XML.Config.XmlSerializationSettings DontWrapRawXml() { }
        [System.ObsoleteAttribute(@"Use `configuration.UseSerialization<XmlSerializer>().Namespace(namespaceToUse)`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public NServiceBus.Serializers.XML.Config.XmlSerializationSettings Namespace(string namespaceToUse) { }
        [System.ObsoleteAttribute(@"Use `configuration.UseSerialization<XmlSerializer>().SanitizeInput()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces. Will be removed in version 6.0.0.", true)]
        public NServiceBus.Serializers.XML.Config.XmlSerializationSettings SanitizeInput() { }
    }
}
namespace NServiceBus.Serializers.XML
{
    
    public class XmlMessageSerializer : NServiceBus.Serialization.IMessageSerializer
    {
        public XmlMessageSerializer(NServiceBus.MessageInterfaces.IMessageMapper mapper, NServiceBus.Conventions conventions) { }
        public string ContentType { get; }
        public string Namespace { get; set; }
        public bool SanitizeInput { get; set; }
        public bool SkipWrappingRawXml { get; set; }
        public object[] Deserialize(System.IO.Stream stream, System.Collections.Generic.IList<System.Type> messageTypesToDeserialize = null) { }
        public void Initialize(System.Collections.Generic.IEnumerable<System.Type> types) { }
        public void InitType(System.Type t) { }
        public void Serialize(object message, System.IO.Stream stream) { }
    }
}
namespace NServiceBus.Settings
{
    
    public interface ReadOnlySettings
    {
        void ApplyTo<T>(NServiceBus.ObjectBuilder.IComponentConfig config);
        T Get<T>();
        T Get<T>(string key);
        object Get(string key);
        T GetOrDefault<T>(string key);
        bool HasExplicitValue(string key);
        bool HasExplicitValue<T>();
        bool HasSetting(string key);
        bool HasSetting<T>();
        bool TryGet<T>(out T val);
        bool TryGet<T>(string key, out T val);
    }
    public class ScaleOutSettings
    {
        public void UniqueQueuePerEndpointInstance() { }
        public void UniqueQueuePerEndpointInstance(string discriminator) { }
        public void UseSingleBrokerQueue() { }
        public void UseUniqueBrokerQueuePerMachine() { }
    }
    [System.ObsoleteAttribute("Will be removed in version 6.0.0.", true)]
    public class SerializationSettings
    {
        [System.ObsoleteAttribute("In version 5 multi-message sends was removed. So Wrapping messages is no longer r" +
            "equired. If you are communicating with version 3 ensure you are on the latest 3." +
            "3.x. Will be removed in version 6.0.0.", true)]
        public NServiceBus.Settings.SerializationSettings DontWrapSingleMessages() { }
        [System.ObsoleteAttribute("In version 5 multi-message sends was removed. So Wrapping messages is no longer r" +
            "equired. If you are communicating with version 3 ensure you are on the latest 3." +
            "3.x. Will be removed in version 6.0.0.", true)]
        public NServiceBus.Settings.SerializationSettings WrapSingleMessages() { }
    }
    public class SettingsHolder : NServiceBus.Settings.ReadOnlySettings
    {
        public SettingsHolder() { }
        public void ApplyTo<T>(NServiceBus.ObjectBuilder.IComponentConfig config) { }
        public T Get<T>(string key) { }
        public T Get<T>() { }
        public object Get(string key) { }
        public T GetOrDefault<T>() { }
        public T GetOrDefault<T>(string key) { }
        public bool HasExplicitValue(string key) { }
        public bool HasExplicitValue<T>() { }
        public bool HasSetting(string key) { }
        public bool HasSetting<T>() { }
        public void Set(string key, object value) { }
        public void Set<T>(object value) { }
        public void Set<T>(System.Action value) { }
        public void SetDefault<T>(object value) { }
        public void SetDefault<T>(System.Action value) { }
        public void SetDefault(string key, object value) { }
        public void SetProperty<T>(System.Linq.Expressions.Expression<System.Func<T, object>> property, object value) { }
        public void SetPropertyDefault<T>(System.Linq.Expressions.Expression<System.Func<T, object>> property, object value) { }
        public bool TryGet<T>(out T val) { }
        public bool TryGet<T>(string key, out T val) { }
    }
    public class TransactionSettings
    {
        public NServiceBus.Settings.TransactionSettings DefaultTimeout(System.TimeSpan defaultTimeout) { }
        public NServiceBus.Settings.TransactionSettings Disable() { }
        public NServiceBus.Settings.TransactionSettings DisableDistributedTransactions() { }
        public NServiceBus.Settings.TransactionSettings DoNotWrapHandlersExecutionInATransactionScope() { }
        public NServiceBus.Settings.TransactionSettings Enable() { }
        public NServiceBus.Settings.TransactionSettings EnableDistributedTransactions() { }
        public NServiceBus.Settings.TransactionSettings IsolationLevel(System.Transactions.IsolationLevel isolationLevel) { }
        public NServiceBus.Settings.TransactionSettings WrapHandlersExecutionInATransactionScope() { }
    }
}
namespace NServiceBus.Support
{
    
    public class static RuntimeEnvironment
    {
        public static string MachineName { get; }
        public static System.Func<string> MachineNameAction { get; set; }
    }
}
namespace NServiceBus.Timeout.Core
{
    
    [System.ObsoleteAttribute("Timeout management is an internal concern and cannot be replaced. Will be removed" +
        " in version 6.0.0.", true)]
    public interface IManageTimeouts { }
    public interface IPersistTimeouts
    {
        void Add(NServiceBus.Timeout.Core.TimeoutData timeout);
        System.Collections.Generic.IEnumerable<System.Tuple<string, System.DateTime>> GetNextChunk(System.DateTime startSlice, out System.DateTime nextTimeToRunQuery);
        void RemoveTimeoutBy(System.Guid sagaId);
        bool TryRemove(string timeoutId, out NServiceBus.Timeout.Core.TimeoutData timeoutData);
    }
    public class TimeoutData
    {
        public const string OriginalReplyToAddress = "NServiceBus.Timeout.ReplyToAddress";
        public NServiceBus.Address Destination { get; set; }
        public System.Collections.Generic.Dictionary<string, string> Headers { get; set; }
        public string Id { get; set; }
        public string OwningTimeoutManager { get; set; }
        public System.Guid SagaId { get; set; }
        public byte[] State { get; set; }
        public System.DateTime Time { get; set; }
        public NServiceBus.Unicast.SendOptions ToSendOptions(NServiceBus.Address replyToAddress) { }
        public override string ToString() { }
        public NServiceBus.TransportMessage ToTransportMessage() { }
    }
}
namespace NServiceBus.Transports
{
    
    public abstract class ConfigureTransport : NServiceBus.Features.Feature
    {
        protected ConfigureTransport() { }
        protected abstract string ExampleConnectionStringForErrorMessage { get; }
        protected virtual bool RequiresConnectionString { get; }
        protected abstract void Configure(NServiceBus.Features.FeatureConfigurationContext context, string connectionString);
        protected virtual string GetLocalAddress(NServiceBus.Settings.ReadOnlySettings settings) { }
        protected internal override void Setup(NServiceBus.Features.FeatureConfigurationContext context) { }
    }
    public interface IAuditMessages
    {
        void Audit(NServiceBus.Unicast.SendOptions sendOptions, NServiceBus.TransportMessage message);
    }
    public interface ICreateQueues
    {
        void CreateQueueIfNecessary(NServiceBus.Address address, string account);
    }
    public interface IDeferMessages
    {
        void ClearDeferredMessages(string headerKey, string headerValue);
        void Defer(NServiceBus.TransportMessage message, NServiceBus.Unicast.SendOptions sendOptions);
    }
    public interface IDequeueMessages
    {
        void Init(NServiceBus.Address address, NServiceBus.Unicast.Transport.TransactionSettings transactionSettings, System.Func<NServiceBus.TransportMessage, bool> tryProcessMessage, System.Action<NServiceBus.TransportMessage, System.Exception> endProcessMessage);
        void Start(int maximumConcurrencyLevel);
        void Stop();
    }
    public interface IManageSubscriptions
    {
        void Subscribe(System.Type eventType, NServiceBus.Address publisherAddress);
        void Unsubscribe(System.Type eventType, NServiceBus.Address publisherAddress);
    }
    public interface IPublishMessages
    {
        void Publish(NServiceBus.TransportMessage message, NServiceBus.Unicast.PublishOptions publishOptions);
    }
    public interface ISendMessages
    {
        void Send(NServiceBus.TransportMessage message, NServiceBus.Unicast.SendOptions sendOptions);
    }
    public abstract class TransportDefinition
    {
        public bool HasNativePubSubSupport { get; set; }
        public bool HasSupportForCentralizedPubSub { get; set; }
        public System.Nullable<bool> HasSupportForDistributedTransactions { get; set; }
        public bool RequireOutboxConsent { get; set; }
        protected internal void Configure(NServiceBus.BusConfiguration config) { }
    }
}
namespace NServiceBus.Transports.Msmq.Config
{
    
    public class MsmqSettings
    {
        public MsmqSettings() { }
        public System.TimeSpan TimeToReachQueue { get; set; }
        public bool UseConnectionCache { get; set; }
        public bool UseDeadLetterQueue { get; set; }
        public bool UseJournalQueue { get; set; }
        public bool UseTransactionalQueues { get; set; }
    }
}
namespace NServiceBus.Transports.Msmq
{
    
    public class HeaderInfo
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
    public class MsmqDequeueStrategy : NServiceBus.Transports.IDequeueMessages, System.IDisposable
    {
        public MsmqDequeueStrategy(NServiceBus.Configure configure, NServiceBus.CriticalError criticalError, NServiceBus.Transports.Msmq.MsmqUnitOfWork unitOfWork) { }
        public NServiceBus.Address ErrorQueue { get; set; }
        public void Dispose() { }
        public void Init(NServiceBus.Address address, NServiceBus.Unicast.Transport.TransactionSettings settings, System.Func<NServiceBus.TransportMessage, bool> tryProcessMessage, System.Action<NServiceBus.TransportMessage, System.Exception> endProcessMessage) { }
        public void Start(int maximumConcurrencyLevel) { }
        public void Stop() { }
    }
    public class MsmqMessageSender : NServiceBus.Transports.ISendMessages
    {
        public NServiceBus.Transports.Msmq.Config.MsmqSettings Settings { get; set; }
        public bool SuppressDistributedTransactions { get; set; }
        public NServiceBus.Transports.Msmq.MsmqUnitOfWork UnitOfWork { get; set; }
        public void Send(NServiceBus.TransportMessage message, NServiceBus.Unicast.SendOptions sendOptions) { }
    }
    public class MsmqUnitOfWork : System.IDisposable
    {
        public MsmqUnitOfWork() { }
        public System.Messaging.MessageQueueTransaction Transaction { get; }
        public void Dispose() { }
        public bool HasActiveTransaction() { }
    }
    [System.ObsoleteAttribute("`MsmqUtilities` was never intended to be exposed as part of the public API. PLeas" +
        "e copy the required functionality into your codebase. Will be removed in version" +
        " 6.0.0.", true)]
    public class MsmqUtilities { }
}
namespace NServiceBus.Unicast.Behaviors
{
    
    public class MessageHandler
    {
        public object Instance { get; set; }
        public System.Action<object, object> Invocation { get; set; }
    }
}
namespace NServiceBus.Unicast
{
    
    public class static BuilderExtensions
    {
        public static void ForEach<T>(this NServiceBus.ObjectBuilder.IBuilder builder, System.Action<T> action) { }
    }
    public class BusAsyncResult : System.IAsyncResult
    {
        public BusAsyncResult(System.AsyncCallback callback, object state) { }
        public object AsyncState { get; }
        public System.Threading.WaitHandle AsyncWaitHandle { get; }
        public bool CompletedSynchronously { get; }
        public bool IsCompleted { get; }
        public void Complete(int errorCode, params object[] messages) { }
    }
    [System.ObsoleteAttribute("Please use `ICallback` instead. Will be removed in version 6.0.0.", true)]
    public class Callback { }
    public abstract class DeliveryOptions
    {
        protected DeliveryOptions() { }
        public bool EnforceMessagingBestPractices { get; set; }
        public bool EnlistInReceiveTransaction { get; set; }
        public NServiceBus.Address ReplyToAddress { get; set; }
    }
    [System.ObsoleteAttribute("Please use `Use the pipeline and replace the InvokeHandlers step` instead. Will b" +
        "e removed in version 6.0.0.", true)]
    public interface IMessageDispatcherFactory
    {
        bool CanDispatch(System.Type handler);
        System.Collections.Generic.IEnumerable<System.Action> GetDispatcher(System.Type messageHandlerType, NServiceBus.ObjectBuilder.IBuilder builder, object toHandle);
    }
    [System.ObsoleteAttribute("Not a public API. Please use `MessageHandlerRegistry` instead. Will be treated as" +
        " an error from version 6.0.0. Will be removed in version 6.0.0.", false)]
    public interface IMessageHandlerRegistry
    {
        System.Collections.Generic.IEnumerable<System.Type> GetHandlerTypes(System.Type messageType);
        System.Collections.Generic.IEnumerable<System.Type> GetMessageTypes();
        void InvokeHandle(object handler, object message);
        void InvokeTimeout(object handler, object state);
    }
    [System.ObsoleteAttribute("Please use `IBus` instead. Will be removed in version 6.0.0.", true)]
    public class IUnicastBus { }
    public class MessageContext : NServiceBus.IMessageContext
    {
        public MessageContext(NServiceBus.TransportMessage transportMessage) { }
        public System.DateTime TimeSent { get; }
    }
    public class MessageEventArgs : System.EventArgs
    {
        public MessageEventArgs(object msg) { }
        public object Message { get; }
    }
    public class MessageHandlerRegistry : NServiceBus.Unicast.IMessageHandlerRegistry
    {
        public void CacheMethodForHandler(System.Type handler, System.Type messageType) { }
        public void Clear() { }
        public System.Collections.Generic.IEnumerable<System.Type> GetHandlerTypes(System.Type messageType) { }
        public System.Collections.Generic.IEnumerable<System.Type> GetMessageTypes() { }
        public void InvokeHandle(object handler, object message) { }
        public void InvokeTimeout(object handler, object state) { }
        public void RegisterHandler(System.Type handlerType) { }
    }
    public class MessagesEventArgs : System.EventArgs
    {
        public MessagesEventArgs(object[] messages) { }
        public object[] Messages { get; }
    }
    public class PublishOptions : NServiceBus.Unicast.DeliveryOptions
    {
        public PublishOptions(System.Type eventType) { }
        public System.Type EventType { get; }
    }
    public class ReplyOptions : NServiceBus.Unicast.SendOptions
    {
        public ReplyOptions(NServiceBus.Address destination, string correlationId) { }
    }
    public class SendOptions : NServiceBus.Unicast.DeliveryOptions
    {
        public SendOptions(NServiceBus.Address destination) { }
        public SendOptions(string destination) { }
        public string CorrelationId { get; set; }
        public System.Nullable<System.TimeSpan> DelayDeliveryWith { get; set; }
        public System.Nullable<System.DateTime> DeliverAt { get; set; }
        public NServiceBus.Address Destination { get; set; }
        public System.Nullable<System.TimeSpan> TimeToBeReceived { get; set; }
    }
    public class UnicastBus : NServiceBus.IBus, NServiceBus.IInMemoryOperations, NServiceBus.IManageMessageHeaders, NServiceBus.ISendOnlyBus, NServiceBus.IStartableBus, System.IDisposable
    {
        public UnicastBus() { }
        public NServiceBus.ObjectBuilder.IBuilder Builder { get; set; }
        public NServiceBus.Configure Configure { get; set; }
        public NServiceBus.CriticalError CriticalError { get; set; }
        public NServiceBus.IMessageContext CurrentMessageContext { get; }
        public bool DoNotStartTransport { get; set; }
        public System.Func<object, string, string> GetHeaderAction { get; }
        [System.ObsoleteAttribute("We have introduced a more explicit API to set the host identifier, see busConfigu" +
            "ration.UniquelyIdentifyRunningInstance(). Will be treated as an error from versi" +
            "on 6.0.0. Will be removed in version 7.0.0.", false)]
        public NServiceBus.Hosting.HostInformation HostInformation { get; set; }
        [System.ObsoleteAttribute("InMemory has been removed from the core. Will be removed in version 6.0.0.", true)]
        public NServiceBus.IInMemoryOperations InMemory { get; }
        public NServiceBus.Address InputAddress { get; set; }
        public NServiceBus.MessageInterfaces.IMessageMapper MessageMapper { get; set; }
        public NServiceBus.Unicast.Routing.StaticMessageRouter MessageRouter { get; set; }
        public NServiceBus.Transports.ISendMessages MessageSender { get; set; }
        public System.Collections.Generic.IDictionary<string, string> OutgoingHeaders { get; }
        public bool PropagateReturnAddressOnSend { get; set; }
        public System.Action<object, string, string> SetHeaderAction { get; }
        public NServiceBus.Settings.ReadOnlySettings Settings { get; set; }
        public NServiceBus.Transports.IManageSubscriptions SubscriptionManager { get; set; }
        public NServiceBus.Unicast.Transport.ITransport Transport { get; set; }
        public NServiceBus.ICallback Defer(System.TimeSpan delay, object message) { }
        public NServiceBus.ICallback Defer(System.DateTime processAt, object message) { }
        public void Dispose() { }
        public void DoNotContinueDispatchingCurrentMessageToHandlers() { }
        public void ForwardCurrentMessageTo(string destination) { }
        public void HandleCurrentMessageLater() { }
        public void Publish<T>(System.Action<T> messageConstructor) { }
        public virtual void Publish<T>() { }
        public virtual void Publish<T>(T message) { }
        [System.ObsoleteAttribute("InMemory.Raise has been removed from the core. Will be removed in version 6.0.0.", true)]
        public void Raise<T>(System.Action<T> messageConstructor) { }
        [System.ObsoleteAttribute("InMemory.Raise has been removed from the core. Will be removed in version 6.0.0.", true)]
        public void Raise<T>(T @event) { }
        public void Reply(object message) { }
        public void Reply<T>(System.Action<T> messageConstructor) { }
        public void Return<T>(T errorCode) { }
        public NServiceBus.ICallback Send<T>(System.Action<T> messageConstructor) { }
        public NServiceBus.ICallback Send(object message) { }
        public NServiceBus.ICallback Send<T>(string destination, System.Action<T> messageConstructor) { }
        public NServiceBus.ICallback Send<T>(NServiceBus.Address address, System.Action<T> messageConstructor) { }
        public NServiceBus.ICallback Send(string destination, object message) { }
        public NServiceBus.ICallback Send(NServiceBus.Address address, object message) { }
        public NServiceBus.ICallback Send<T>(string destination, string correlationId, System.Action<T> messageConstructor) { }
        public NServiceBus.ICallback Send<T>(NServiceBus.Address address, string correlationId, System.Action<T> messageConstructor) { }
        public NServiceBus.ICallback Send(string destination, string correlationId, object message) { }
        public NServiceBus.ICallback Send(NServiceBus.Address address, string correlationId, object message) { }
        public NServiceBus.ICallback SendLocal<T>(System.Action<T> messageConstructor) { }
        public NServiceBus.ICallback SendLocal(object message) { }
        public NServiceBus.IBus Start() { }
        public void Subscribe<T>() { }
        public virtual void Subscribe(System.Type messageType) { }
        public void Unsubscribe<T>() { }
        public virtual void Unsubscribe(System.Type messageType) { }
    }
}
namespace NServiceBus.Unicast.Config
{
    
    [System.ObsoleteAttribute("Please use `Configure` instead. Will be removed in version 6.0.0.", true)]
    public class ConfigUnicastBus { }
}
namespace NServiceBus.Unicast.Messages
{
    
    public class LogicalMessage
    {
        public System.Collections.Generic.Dictionary<string, string> Headers { get; }
        public object Instance { get; }
        public System.Type MessageType { get; }
        public NServiceBus.Unicast.Messages.MessageMetadata Metadata { get; }
        public void UpdateMessageInstance(object newInstance) { }
    }
    public class LogicalMessageFactory
    {
        public LogicalMessageFactory(NServiceBus.Unicast.Messages.MessageMetadataRegistry messageMetadataRegistry, NServiceBus.MessageInterfaces.IMessageMapper messageMapper, NServiceBus.Pipeline.PipelineExecutor pipelineExecutor) { }
        public NServiceBus.Unicast.Messages.LogicalMessage Create(object message) { }
        public NServiceBus.Unicast.Messages.LogicalMessage Create(System.Type messageType, object message, System.Collections.Generic.Dictionary<string, string> headers) { }
        public NServiceBus.Unicast.Messages.LogicalMessage CreateControl(System.Collections.Generic.Dictionary<string, string> headers) { }
    }
    public class MessageMetadata
    {
        public System.Collections.Generic.IEnumerable<System.Type> MessageHierarchy { get; }
        public System.Type MessageType { get; }
        public bool Recoverable { get; }
        public System.TimeSpan TimeToBeReceived { get; }
        public override string ToString() { }
    }
    public class MessageMetadataRegistry
    {
        public NServiceBus.Unicast.Messages.MessageMetadata GetMessageMetadata(System.Type messageType) { }
        public NServiceBus.Unicast.Messages.MessageMetadata GetMessageMetadata(string messageTypeIdentifier) { }
    }
}
namespace NServiceBus.Unicast.Queuing
{
    
    [System.ObsoleteAttribute("Since the case where this exception was thrown should not be handled by consumers" +
        " of the API it has been removed. Will be removed in version 6.0.0.", true)]
    public class FailedToSendMessageException : System.Exception { }
    public interface IWantQueueCreated
    {
        NServiceBus.Address Address { get; }
        bool ShouldCreateQueue();
    }
    public class QueueNotFoundException : System.Exception
    {
        public QueueNotFoundException() { }
        public QueueNotFoundException(NServiceBus.Address queue, string message, System.Exception inner) { }
        protected QueueNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public NServiceBus.Address Queue { get; set; }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
}
namespace NServiceBus.Unicast.Routing
{
    
    public class StaticMessageRouter
    {
        public StaticMessageRouter(System.Collections.Generic.IEnumerable<System.Type> knownMessages) { }
        public bool SubscribeToPlainMessages { get; set; }
        public System.Collections.Generic.List<NServiceBus.Address> GetDestinationFor(System.Type messageType) { }
        public void RegisterEventRoute(System.Type eventType, NServiceBus.Address endpointAddress) { }
        public void RegisterMessageRoute(System.Type messageType, NServiceBus.Address endpointAddress) { }
    }
}
namespace NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions
{
    
    public interface ISubscriptionStorage
    {
        System.Collections.Generic.IEnumerable<NServiceBus.Address> GetSubscriberAddressesForMessage(System.Collections.Generic.IEnumerable<NServiceBus.Unicast.Subscriptions.MessageType> messageTypes);
        void Init();
        void Subscribe(NServiceBus.Address client, System.Collections.Generic.IEnumerable<NServiceBus.Unicast.Subscriptions.MessageType> messageTypes);
        void Unsubscribe(NServiceBus.Address client, System.Collections.Generic.IEnumerable<NServiceBus.Unicast.Subscriptions.MessageType> messageTypes);
    }
}
namespace NServiceBus.Unicast.Subscriptions
{
    
    public class MessageType
    {
        public MessageType(System.Type type) { }
        public MessageType(string messageTypeString) { }
        public MessageType(string typeName, string versionString) { }
        public MessageType(string typeName, System.Version version) { }
        public string TypeName { get; }
        public System.Version Version { get; }
        public bool Equals(NServiceBus.Unicast.Subscriptions.MessageType other) { }
        public override bool Equals(object obj) { }
        public override int GetHashCode() { }
        public override string ToString() { }
    }
    public class SubscriptionEventArgs : System.EventArgs
    {
        public string MessageType { get; set; }
        public NServiceBus.Address SubscriberReturnAddress { get; set; }
    }
}
namespace NServiceBus.Unicast.Transport
{
    
    public class static ControlMessage
    {
        public static NServiceBus.TransportMessage Create() { }
    }
    public class FailedMessageProcessingEventArgs : System.EventArgs
    {
        public FailedMessageProcessingEventArgs(NServiceBus.TransportMessage m, System.Exception ex) { }
        public NServiceBus.TransportMessage Message { get; }
        public System.Exception Reason { get; }
    }
    public class FinishedMessageProcessingEventArgs : System.EventArgs
    {
        public FinishedMessageProcessingEventArgs(NServiceBus.TransportMessage m) { }
        public NServiceBus.TransportMessage Message { get; }
    }
    public interface ITransport
    {
        int MaximumConcurrencyLevel { get; }
        int MaximumMessageThroughputPerSecond { get; }
        public event System.EventHandler<NServiceBus.Unicast.Transport.FailedMessageProcessingEventArgs> FailedMessageProcessing;
        public event System.EventHandler<NServiceBus.Unicast.Transport.FinishedMessageProcessingEventArgs> FinishedMessageProcessing;
        public event System.EventHandler<NServiceBus.Unicast.Transport.StartedMessageProcessingEventArgs> StartedMessageProcessing;
        public event System.EventHandler<NServiceBus.Unicast.Transport.TransportMessageReceivedEventArgs> TransportMessageReceived;
        void AbortHandlingCurrentMessage();
        void ChangeMaximumConcurrencyLevel(int maximumConcurrencyLevel);
        void ChangeMaximumMessageThroughputPerSecond(int maximumMessageThroughputPerSecond);
        void Start(NServiceBus.Address localAddress);
        void Stop();
    }
    public class StartedMessageProcessingEventArgs : System.EventArgs
    {
        public StartedMessageProcessingEventArgs(NServiceBus.TransportMessage m) { }
        public NServiceBus.TransportMessage Message { get; }
    }
    public class TransactionSettings
    {
        public TransactionSettings(bool isTransactional, System.TimeSpan transactionTimeout, System.Transactions.IsolationLevel isolationLevel, int maxRetries, bool suppressDistributedTransactions, bool doNotWrapHandlersExecutionInATransactionScope) { }
        public bool DoNotWrapHandlersExecutionInATransactionScope { get; set; }
        public System.Transactions.IsolationLevel IsolationLevel { get; set; }
        public bool IsTransactional { get; set; }
        public int MaxRetries { get; set; }
        public bool SuppressDistributedTransactions { get; set; }
        public System.TimeSpan TransactionTimeout { get; set; }
    }
    public class TransportMessageAvailableEventArgs : System.EventArgs
    {
        public TransportMessageAvailableEventArgs(NServiceBus.TransportMessage m) { }
        public NServiceBus.TransportMessage Message { get; }
    }
    [System.ObsoleteAttribute("Since the case where this exception was thrown should not be handled by consumers" +
        " of the API it has been removed. Will be removed in version 6.0.0.", true)]
    public class TransportMessageHandlingFailedException : System.Exception { }
    public class TransportMessageReceivedEventArgs : System.EventArgs
    {
        public TransportMessageReceivedEventArgs(NServiceBus.TransportMessage m) { }
        public NServiceBus.TransportMessage Message { get; }
    }
    public class TransportReceiver : NServiceBus.Unicast.Transport.ITransport, System.IDisposable
    {
        public TransportReceiver(NServiceBus.Unicast.Transport.TransactionSettings transactionSettings, int maximumConcurrencyLevel, int maximumThroughput, NServiceBus.Transports.IDequeueMessages receiver, NServiceBus.Faults.IManageMessageFailures manageMessageFailures, NServiceBus.Settings.ReadOnlySettings settings, NServiceBus.Configure config) { }
        public NServiceBus.Faults.IManageMessageFailures FailureManager { get; set; }
        public int MaximumConcurrencyLevel { get; }
        public int MaximumMessageThroughputPerSecond { get; }
        public NServiceBus.Transports.IDequeueMessages Receiver { get; set; }
        public NServiceBus.Unicast.Transport.TransactionSettings TransactionSettings { get; }
        public event System.EventHandler<NServiceBus.Unicast.Transport.FailedMessageProcessingEventArgs> FailedMessageProcessing;
        public event System.EventHandler<NServiceBus.Unicast.Transport.FinishedMessageProcessingEventArgs> FinishedMessageProcessing;
        public event System.EventHandler<NServiceBus.Unicast.Transport.StartedMessageProcessingEventArgs> StartedMessageProcessing;
        public event System.EventHandler<NServiceBus.Unicast.Transport.TransportMessageReceivedEventArgs> TransportMessageReceived;
        public void AbortHandlingCurrentMessage() { }
        public void ChangeMaximumConcurrencyLevel(int maximumConcurrencyLevel) { }
        public void ChangeMaximumMessageThroughputPerSecond(int maximumMessageThroughputPerSecond) { }
        public void Dispose() { }
        public void Start(NServiceBus.Address address) { }
        public void Stop() { }
    }
}
namespace NServiceBus.UnitOfWork
{
    
    public interface IManageUnitsOfWork
    {
        void Begin();
        void End(System.Exception ex = null);
    }
}
namespace NServiceBus.Utils
{
    
    [System.ObsoleteAttribute("This class was never intended to be exposed as part of the public API. Will be re" +
        "moved in version 6.0.0.", true)]
    public class FileVersionRetriever { }
    [System.ObsoleteAttribute("This class was never intended to be exposed as part of the public API. Will be re" +
        "moved in version 6.0.0.", true)]
    public class static RegistryReader<T> { }
}
namespace System.Threading.Tasks.Schedulers
{
    
    [System.ObsoleteAttribute("This class was never intended to be exposed as part of the public API. Will be re" +
        "moved in version 6.0.0.", true)]
    public sealed class MTATaskScheduler { }
}