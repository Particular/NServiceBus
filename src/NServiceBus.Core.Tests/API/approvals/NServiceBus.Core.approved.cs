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
        public string Machine { get; }
        public string Queue { get; }
        public override bool Equals(object obj) { }
        public override int GetHashCode() { }
        public static void IgnoreMachineName() { }
        public static void OverrideDefaultMachine(string machineName) { }
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
        public BinarySerializer() { }
        protected internal override System.Type ProvidedByFeature() { }
    }
    public class BsonSerializer : NServiceBus.Serialization.SerializationDefinition
    {
        public BsonSerializer() { }
        protected internal override System.Type ProvidedByFeature() { }
    }
    public class static Bus
    {
        public static NServiceBus.IStartableBus Create(NServiceBus.BusConfiguration configuration) { }
        public static NServiceBus.ISendOnlyBus CreateSendOnly(NServiceBus.BusConfiguration configuration) { }
    }
    public class BusAsyncResultEventArgs : System.EventArgs
    {
        public BusAsyncResultEventArgs() { }
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
        public CompletionResult() { }
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
        public Configure(NServiceBus.Settings.SettingsHolder settings, NServiceBus.ObjectBuilder.Common.IContainer container, System.Collections.Generic.List<System.Action<NServiceBus.ObjectBuilder.IConfigureComponents>> registrations, NServiceBus.Pipeline.PipelineSettings pipeline) { }
        public NServiceBus.ObjectBuilder.IBuilder Builder { get; }
        public NServiceBus.Address LocalAddress { get; }
        public NServiceBus.Settings.SettingsHolder Settings { get; }
        public System.Collections.Generic.IList<System.Type> TypesToScan { get; }
    }
    public class static ConfigureCriticalErrorAction
    {
        public static void DefineCriticalErrorAction(this NServiceBus.BusConfiguration busConfiguration, System.Action<string, System.Exception> onCriticalError) { }
    }
    public class static ConfigureFileShareDataBus
    {
        public static NServiceBus.DataBus.DataBusExtentions<NServiceBus.FileShareDataBus> BasePath(this NServiceBus.DataBus.DataBusExtentions<NServiceBus.FileShareDataBus> config, string basePath) { }
    }
    public class static ConfigureHandlerSettings
    {
        public static void InitializeHandlerProperty<THandler>(this NServiceBus.BusConfiguration config, string property, object value) { }
    }
    public class static ConfigureInMemoryFaultManagement
    {
        public static void DiscardFailedMessagesInsteadOfSendingToErrorQueue(this NServiceBus.BusConfiguration config) { }
    }
    public class static ConfigureLicenseExtensions
    {
        public static void License(this NServiceBus.BusConfiguration config, string licenseText) { }
        public static void LicensePath(this NServiceBus.BusConfiguration config, string licenseFile) { }
    }
    public class static ConfigurePurging
    {
        public static void PurgeOnStartup(this NServiceBus.BusConfiguration config, bool value) { }
        public static bool PurgeOnStartup(this NServiceBus.Configure config) { }
    }
    public class static ConfigureQueueCreation
    {
        public static bool CreateQueues(this NServiceBus.Configure config) { }
        public static void DoNotCreateQueues(this NServiceBus.BusConfiguration config) { }
    }
    public class static ConfigureRijndaelEncryptionService
    {
        public static void RegisterEncryptionService(this NServiceBus.BusConfiguration config, System.Func<NServiceBus.ObjectBuilder.IBuilder, NServiceBus.Encryption.IEncryptionService> func) { }
        public static void RijndaelEncryptionService(this NServiceBus.BusConfiguration config) { }
        public static void RijndaelEncryptionService(this NServiceBus.BusConfiguration config, string encryptionKey, System.Collections.Generic.List<string> expiredKeys = null) { }
    }
    public class static ConfigureTransportConnectionString
    {
        public static string TransportConnectionString(this NServiceBus.Configure config) { }
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
        public EncryptedValue() { }
        public string Base64Iv { get; set; }
        public string EncryptedBase64Value { get; set; }
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
    public sealed class ExpressAttribute : System.Attribute
    {
        public ExpressAttribute() { }
    }
    public class static ExtensionMethods
    {
        public static object CurrentMessageBeingHandled { get; set; }
        public static string GetMessageHeader(this NServiceBus.IBus bus, object msg, string key) { }
        public static void SetMessageHeader(this NServiceBus.ISendOnlyBus bus, object msg, string key, string value) { }
    }
    public class FileShareDataBus : NServiceBus.DataBus.DataBusDefinition
    {
        public FileShareDataBus() { }
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
    }
    public class static HostInfoConfigurationExtensions
    {
        public static NServiceBus.HostInfoSettings UniquelyIdentifyRunningInstance(this NServiceBus.BusConfiguration config) { }
    }
    public class HostInfoSettings
    {
        public NServiceBus.HostInfoSettings UsingCustomDisplayName(string displayName) { }
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
    public class static InstallConfigExtensions
    {
        public static void EnableInstallers(this NServiceBus.BusConfiguration config, string username = null) { }
    }
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
        public JsonSerializer() { }
        protected internal override System.Type ProvidedByFeature() { }
    }
    public class static JsonSerializerConfigurationExtensions
    {
        public static void Encoding(this NServiceBus.Serialization.SerializationExtentions<NServiceBus.JsonSerializer> config, System.Text.Encoding encoding) { }
    }
    public class static LoadMessageHandlersExtentions
    {
        public static void LoadMessageHandlers<TFirst>(this NServiceBus.BusConfiguration config) { }
        public static void LoadMessageHandlers<T>(this NServiceBus.BusConfiguration config, NServiceBus.First<T> order) { }
    }
    public class MessageDeserializationException : System.Runtime.Serialization.SerializationException
    {
        public MessageDeserializationException(string transportMessageId, System.Exception innerException) { }
        protected MessageDeserializationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public enum MessageIntentEnum
    {
        Send = 1,
        Publish = 2,
        Subscribe = 3,
        Unsubscribe = 4,
        Reply = 5,
    }
    public class MsmqTransport : NServiceBus.Transports.TransportDefinition
    {
        public MsmqTransport() { }
        protected internal override void Configure(NServiceBus.BusConfiguration config) { }
    }
    public class Order
    {
        public Order() { }
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
        [System.ObsoleteAttribute("Please use `UsePersistence<T, S>()` instead. Will be removed in version 7.0.0.", true)]
        public NServiceBus.PersistenceExtentions For(params NServiceBus.Persistence.Storage[] specificStorages) { }
    }
    public class PersistenceExtentions<T> : NServiceBus.PersistenceExtentions
        where T : NServiceBus.Persistence.PersistenceDefinition
    {
        public PersistenceExtentions(NServiceBus.Settings.SettingsHolder settings) { }
        protected PersistenceExtentions(NServiceBus.Settings.SettingsHolder settings, System.Type storageType) { }
        [System.ObsoleteAttribute("Please use `UsePersistence<T, S>()` instead. Will be removed in version 7.0.0.", true)]
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
    }
    public class Schedule
    {
        public Schedule(NServiceBus.ObjectBuilder.IBuilder builder) { }
        public void Every(System.TimeSpan timeSpan, System.Action task) { }
        public void Every(System.TimeSpan timeSpan, string name, System.Action task) { }
    }
    public class static SecondLevelRetriesConfigExtensions
    {
        public static NServiceBus.SecondLevelRetries.Config.SecondLevelRetriesSettings SecondLevelRetries(this NServiceBus.BusConfiguration config) { }
    }
    public class static SerializationConfigExtensions
    {
        public static NServiceBus.Serialization.SerializationExtentions<T> UseSerialization<T>(this NServiceBus.BusConfiguration config)
            where T : NServiceBus.Serialization.SerializationDefinition { }
        public static void UseSerialization(this NServiceBus.BusConfiguration config, System.Type serializerType) { }
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
    public class static TransactionSettingsExtentions
    {
        public static NServiceBus.Settings.TransactionSettings Transactions(this NServiceBus.BusConfiguration config) { }
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
        public byte[] Body { get; set; }
        public string CorrelationId { get; set; }
        public System.Collections.Generic.Dictionary<string, string> Headers { get; }
        public string Id { get; }
        public NServiceBus.MessageIntentEnum MessageIntent { get; set; }
        public bool Recoverable { get; set; }
        public NServiceBus.Address ReplyToAddress { get; }
        public System.TimeSpan TimeToBeReceived { get; set; }
    }
    public class static UseDataBusExtensions
    {
        public static NServiceBus.DataBus.DataBusExtentions<T> UseDataBus<T>(this NServiceBus.BusConfiguration config)
            where T : NServiceBus.DataBus.DataBusDefinition, new () { }
        public static NServiceBus.DataBus.DataBusExtentions UseDataBus(this NServiceBus.BusConfiguration config, System.Type dataBusType) { }
    }
    public class static UseTransportExtensions
    {
        public static NServiceBus.TransportExtensions<T> UseTransport<T>(this NServiceBus.BusConfiguration busConfiguration)
            where T : NServiceBus.Transports.TransportDefinition, new () { }
        public static NServiceBus.TransportExtensions UseTransport(this NServiceBus.BusConfiguration busConfiguration, System.Type transportDefinitionType) { }
    }
    public class WireEncryptedString : System.Runtime.Serialization.ISerializable
    {
        public WireEncryptedString() { }
        public WireEncryptedString(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        [System.ObsoleteAttribute("No longer required. Will be removed in version 7.0.0.", true)]
        public string Base64Iv { get; set; }
        [System.ObsoleteAttribute("No longer required. Will be removed in version 7.0.0.", true)]
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
        public XmlSerializer() { }
        protected internal override System.Type ProvidedByFeature() { }
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
        public AuditConfig() { }
        [System.Configuration.ConfigurationPropertyAttribute("OverrideTimeToBeReceived", IsRequired=false)]
        public System.TimeSpan OverrideTimeToBeReceived { get; set; }
        [System.Configuration.ConfigurationPropertyAttribute("QueueName", IsRequired=false)]
        public string QueueName { get; set; }
    }
    public interface IWantToRunWhenConfigurationIsComplete
    {
        void Run(NServiceBus.Configure config);
    }
    public class Logging : System.Configuration.ConfigurationSection
    {
        public Logging() { }
        [System.Configuration.ConfigurationPropertyAttribute("Threshold", DefaultValue="Info", IsRequired=true)]
        public string Threshold { get; set; }
    }
    public class MasterNodeConfig : System.Configuration.ConfigurationSection
    {
        public MasterNodeConfig() { }
        [System.Configuration.ConfigurationPropertyAttribute("Node", IsRequired=true)]
        public string Node { get; set; }
    }
    public class MessageEndpointMapping : System.Configuration.ConfigurationElement, System.IComparable<NServiceBus.Config.MessageEndpointMapping>
    {
        public MessageEndpointMapping() { }
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
        public MessageEndpointMappingCollection() { }
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
        public MessageForwardingInCaseOfFaultConfig() { }
        [System.Configuration.ConfigurationPropertyAttribute("ErrorQueue", IsRequired=true)]
        public string ErrorQueue { get; set; }
    }
    public class MsmqSubscriptionStorageConfig : System.Configuration.ConfigurationSection
    {
        public MsmqSubscriptionStorageConfig() { }
        [System.Configuration.ConfigurationPropertyAttribute("Queue", IsRequired=true)]
        public string Queue { get; set; }
    }
    public class RijndaelEncryptionServiceConfig : System.Configuration.ConfigurationSection
    {
        public RijndaelEncryptionServiceConfig() { }
        [System.Configuration.ConfigurationPropertyAttribute("ExpiredKeys", IsRequired=false)]
        public NServiceBus.Config.RijndaelExpiredKeyCollection ExpiredKeys { get; set; }
        [System.Configuration.ConfigurationPropertyAttribute("Key", IsRequired=true)]
        public string Key { get; set; }
    }
    public class RijndaelExpiredKey : System.Configuration.ConfigurationElement, System.IComparable<NServiceBus.Config.RijndaelExpiredKey>
    {
        public RijndaelExpiredKey() { }
        [System.Configuration.ConfigurationPropertyAttribute("Key", IsRequired=true)]
        public string Key { get; set; }
    }
    public class RijndaelExpiredKeyCollection : System.Configuration.ConfigurationElementCollection
    {
        public RijndaelExpiredKeyCollection() { }
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
        public TransportConfig() { }
        [System.Configuration.ConfigurationPropertyAttribute("MaximumConcurrencyLevel", DefaultValue=1, IsRequired=false)]
        public int MaximumConcurrencyLevel { get; set; }
        [System.Configuration.ConfigurationPropertyAttribute("MaximumMessageThroughputPerSecond", DefaultValue=0, IsRequired=false)]
        public int MaximumMessageThroughputPerSecond { get; set; }
        [System.Configuration.ConfigurationPropertyAttribute("MaxRetries", DefaultValue=5, IsRequired=false)]
        public int MaxRetries { get; set; }
    }
    public class UnicastBusConfig : System.Configuration.ConfigurationSection
    {
        public UnicastBusConfig() { }
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
    public class DefaultConfigurationSource : NServiceBus.Config.ConfigurationSource.IConfigurationSource
    {
        public DefaultConfigurationSource() { }
    }
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
        protected ContainerDefinition() { }
        public abstract NServiceBus.ObjectBuilder.Common.IContainer CreateContainer(NServiceBus.Settings.ReadOnlySettings settings);
    }
}
namespace NServiceBus.DataBus
{
    public abstract class DataBusDefinition
    {
        protected DataBusDefinition() { }
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
        public FeatureDiagnosticData() { }
        public bool Active { get; }
        public System.Collections.Generic.IList<System.Collections.Generic.List<string>> Dependencies { get; }
        public bool DependenciesAreMeet { get; set; }
        public bool EnabledByDefault { get; }
        public string Name { get; }
        public NServiceBus.Features.PrerequisiteStatus PrerequisiteStatus { get; }
        public System.Collections.Generic.IList<System.Type> StartupTasks { get; }
        public string Version { get; }
    }
    public class FeaturesReport
    {
        public System.Collections.Generic.IList<NServiceBus.Features.FeatureDiagnosticData> Features { get; }
    }
    public abstract class FeatureStartupTask
    {
        protected FeatureStartupTask() { }
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
namespace NServiceBus.Hosting
{
    public class HostInformation
    {
        public HostInformation(System.Guid hostId, string displayName) { }
        public HostInformation(System.Guid hostId, string displayName, System.Collections.Generic.Dictionary<string, string> properties) { }
        public string DisplayName { get; }
        public System.Guid HostId { get; }
        public System.Collections.Generic.Dictionary<string, string> Properties { get; }
    }
}
namespace NServiceBus.Installation
{
    public interface INeedToInstallSomething
    {
        void Install(string identity, NServiceBus.Configure config);
    }
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
        protected LoggingFactoryDefinition() { }
        protected internal abstract NServiceBus.Logging.ILoggerFactory GetLoggingFactory();
    }
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
namespace NServiceBus.Persistence.Legacy
{
    public class MsmqPersistence : NServiceBus.Persistence.PersistenceDefinition { }
}
namespace NServiceBus.Persistence
{
    public abstract class PersistenceDefinition
    {
        protected PersistenceDefinition() { }
        protected void Defaults(System.Action<NServiceBus.Settings.SettingsHolder> action) { }
        [System.ObsoleteAttribute("Please use `HasSupportFor<T>()` instead. Will be removed in version 7.0.0.", true)]
        public bool HasSupportFor(NServiceBus.Persistence.Storage storage) { }
        public bool HasSupportFor<T>()
            where T : NServiceBus.Persistence.StorageType { }
        public bool HasSupportFor(System.Type storageType) { }
        protected void Supports<T>(System.Action<NServiceBus.Settings.SettingsHolder> action)
            where T : NServiceBus.Persistence.StorageType { }
        [System.ObsoleteAttribute("Please use `Supports<T>()` instead. Will be removed in version 7.0.0.", true)]
        protected void Supports(NServiceBus.Persistence.Storage storage, System.Action<NServiceBus.Settings.SettingsHolder> action) { }
    }
    [System.ObsoleteAttribute("Please use `NServiceBus.Persistence.StorageType` instead. Will be removed in vers" +
        "ion 7.0.0.", true)]
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
        public static NServiceBus.Pipeline.WellKnownStep HostInformation;
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
        protected ContainSagaData() { }
        public virtual System.Guid Id { get; set; }
        public virtual string OriginalMessageId { get; set; }
        public virtual string Originator { get; set; }
    }
    public interface IAmStartedByMessages<T> : NServiceBus.IHandleMessages<T> { }
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
        protected IFindSagas() { }
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
        protected Saga() { }
        public NServiceBus.IBus Bus { get; set; }
        public bool Completed { get; }
        public NServiceBus.Saga.IContainSagaData Entity { get; set; }
        protected internal abstract void ConfigureHowToFindSaga(NServiceBus.Saga.IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration);
        protected virtual void MarkAsComplete() { }
        protected virtual void ReplyToOriginator(object message) { }
        [System.ObsoleteAttribute("Construct your message and pass it to the non Action overload. Please use `Saga.R" +
            "eplyToOriginator(object)` instead. Will be removed in version 7.0.0.", true)]
        protected virtual void ReplyToOriginator<TMessage>(System.Action<TMessage> messageConstructor)
            where TMessage : new() { }
        protected void RequestTimeout<TTimeoutMessageType>(System.DateTime at)
            where TTimeoutMessageType : new() { }
        [System.ObsoleteAttribute("Construct your message and pass it to the non Action overload. Please use `Saga.R" +
            "equestTimeout<TTimeoutMessageType>(DateTime, TTimeoutMessageType)` instead. Will" +
            " be removed in version 7.0.0.", true)]
        protected void RequestTimeout<TTimeoutMessageType>(System.DateTime at, System.Action<TTimeoutMessageType> action)
            where TTimeoutMessageType : new() { }
        protected void RequestTimeout<TTimeoutMessageType>(System.DateTime at, TTimeoutMessageType timeoutMessage) { }
        protected void RequestTimeout<TTimeoutMessageType>(System.TimeSpan within)
            where TTimeoutMessageType : new() { }
        [System.ObsoleteAttribute("Construct your message and pass it to the non Action overload. Please use `Saga.R" +
            "equestTimeout<TTimeoutMessageType>(TimeSpan, TTimeoutMessageType)` instead. Will" +
            " be removed in version 7.0.0.", true)]
        protected void RequestTimeout<TTimeoutMessageType>(System.TimeSpan within, System.Action<TTimeoutMessageType> messageConstructor)
            where TTimeoutMessageType : new() { }
        protected void RequestTimeout<TTimeoutMessageType>(System.TimeSpan within, TTimeoutMessageType timeoutMessage) { }
    }
    public abstract class Saga<TSagaData> : NServiceBus.Saga.Saga
        where TSagaData : NServiceBus.Saga.IContainSagaData, new ()
    {
        protected Saga() { }
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
        public UniqueAttribute() { }
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
        [System.ObsoleteAttribute("Please use `context.MessageHandler.Instance` instead. Will be removed in version " +
            "7.0.0.", true)]
        public NServiceBus.Saga.Saga Instance { get; }
        public bool IsNew { get; }
        public bool NotFound { get; }
        public string SagaId { get; }
        [System.ObsoleteAttribute("Please use `.Metadata.SagaType` instead. Will be removed in version 7.0.0.", true)]
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
        protected SerializationDefinition() { }
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
    public class SimpleMessageMapper : NServiceBus.IMessageCreator, NServiceBus.MessageInterfaces.IMessageMapper
    {
        public SimpleMessageMapper() { }
    }
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
        protected internal abstract Newtonsoft.Json.JsonReader CreateJsonReader(System.IO.Stream stream);
        protected internal abstract Newtonsoft.Json.JsonWriter CreateJsonWriter(System.IO.Stream stream);
        public object[] Deserialize(System.IO.Stream stream, System.Collections.Generic.IList<System.Type> messageTypes) { }
        protected internal abstract string GetContentType();
        public void Serialize(object message, System.IO.Stream stream) { }
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
        public TimeoutData() { }
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
        protected TransportDefinition() { }
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
        public HeaderInfo() { }
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
        public MsmqMessageSender() { }
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
}
namespace NServiceBus.Unicast.Behaviors
{
    public class MessageHandler
    {
        public MessageHandler() { }
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
    public abstract class DeliveryOptions
    {
        protected DeliveryOptions() { }
        public bool EnforceMessagingBestPractices { get; set; }
        public bool EnlistInReceiveTransaction { get; set; }
        public NServiceBus.Address ReplyToAddress { get; set; }
    }
    [System.ObsoleteAttribute("Not a public API. Please use `MessageHandlerRegistry` instead. Will be removed in" +
        " version 7.0.0.", true)]
    public interface IMessageHandlerRegistry
    {
        System.Collections.Generic.IEnumerable<System.Type> GetHandlerTypes(System.Type messageType);
        System.Collections.Generic.IEnumerable<System.Type> GetMessageTypes();
        void InvokeHandle(object handler, object message);
        void InvokeTimeout(object handler, object state);
    }
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
    public class MessageHandlerRegistry
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
    public class UnicastBus : NServiceBus.IBus, NServiceBus.IManageMessageHeaders, NServiceBus.ISendOnlyBus, NServiceBus.IStartableBus, System.IDisposable
    {
        public UnicastBus() { }
        public NServiceBus.ObjectBuilder.IBuilder Builder { get; set; }
        public NServiceBus.Configure Configure { get; set; }
        public NServiceBus.CriticalError CriticalError { get; set; }
        public NServiceBus.IMessageContext CurrentMessageContext { get; }
        public bool DoNotStartTransport { get; set; }
        public System.Func<object, string, string> GetHeaderAction { get; }
        [System.ObsoleteAttribute("We have introduced a more explicit API to set the host identifier, see busConfigu" +
            "ration.UniquelyIdentifyRunningInstance(). Will be removed in version 7.0.0.", true)]
        public NServiceBus.Hosting.HostInformation HostInformation { get; set; }
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
        public SubscriptionEventArgs() { }
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