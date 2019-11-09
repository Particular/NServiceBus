// ReSharper disable UnusedTypeParameter
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedParameter.Global

#pragma warning disable 1591

namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading.Tasks;
    using Config.ConfigurationSource;
    using MessageInterfaces;
    using Pipeline;
    using Serialization;
    using Settings;

    public partial interface IMessageHandlerContext
    {
        /// <summary>
        /// Moves the message being handled to the back of the list of available
        /// messages so it can be handled later.
        /// </summary>
        [ObsoleteEx(
            Message = "HandleCurrentMessageLater has been deprecated.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        Task HandleCurrentMessageLater();
    }

    [ObsoleteEx(
           Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
           ReplacementTypeOrMember = "NServiceBus.Encryption.MessageProperty.EncryptionConfigurationExtensions.EnableMessagePropertyEncryption",
           RemoveInVersion = "8",
           TreatAsErrorFromVersion = "7")]
    public static class ConfigureRijndaelEncryptionService
    {
        [ObsoleteEx(
            Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
            ReplacementTypeOrMember = "NServiceBus.Encryption.MessageProperty.EncryptionConfigurationExtensions.EnableMessagePropertyEncryption",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public static void RijndaelEncryptionService(this EndpointConfiguration config)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
            ReplacementTypeOrMember = "NServiceBus.Encryption.MessageProperty.EncryptionConfigurationExtensions.EnableMessagePropertyEncryption",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public static void RijndaelEncryptionService(this EndpointConfiguration config, string encryptionKeyIdentifier, byte[] encryptionKey, IList<byte[]> decryptionKeys = null)

        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
            ReplacementTypeOrMember = "NServiceBus.Encryption.MessageProperty.EncryptionConfigurationExtensions.EnableMessagePropertyEncryption",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public static void RijndaelEncryptionService(this EndpointConfiguration config, string encryptionKeyIdentifier, IDictionary<string, byte[]> keys, IList<byte[]> decryptionKeys = null)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
            ReplacementTypeOrMember = "NServiceBus.Encryption.MessageProperty.EncryptionConfigurationExtensions.EnableMessagePropertyEncryption",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public static void RegisterEncryptionService(this EndpointConfiguration config, Func<IEncryptionService> func)
        {
            throw new NotImplementedException();
        }
    }

    public partial class Conventions
    {
        [ObsoleteEx(
            Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public bool IsEncryptedProperty(PropertyInfo property)
        {
            throw new NotImplementedException();
        }
    }

    public partial class ConventionsBuilder
    {
        [ObsoleteEx(
            Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package. This convention configuration does not work in combination with the NServiceBus.Encryption.MessageProperty package.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public ConventionsBuilder DefiningEncryptedPropertiesAs(Func<PropertyInfo, bool> definesEncryptedProperty)
        {
            throw new NotImplementedException();
        }
    }

    [ObsoleteEx(
         Message = "Setting a custom correlation ID is no longer supported.",
         RemoveInVersion = "8",
         TreatAsErrorFromVersion = "7")]
    public static class CorrelationContextExtensions
    {
        [ObsoleteEx(
            Message = "Setting a custom correlation ID is no longer supported.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public static void SetCorrelationId(this SendOptions options, string correlationId)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Setting a custom correlation ID is no longer supported.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public static void SetCorrelationId(this ReplyOptions options, string correlationId)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Using custom correlation IDs is no longer supported.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public static string GetCorrelationId(this SendOptions options)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Using custom correlation IDs is no longer supported.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public static string GetCorrelationId(this ReplyOptions options)
        {
            throw new NotImplementedException();
        }
    }

    [ObsoleteEx(
        Message = "Performance counters have been released as a separate package: NServiceBus.Metrics.PerformanceCounters",
        RemoveInVersion = "8",
        TreatAsErrorFromVersion = "7")]
    public static class CriticalTimeMonitoringConfig
    {
        [ObsoleteEx(
            Message = "Performance counters have been released as a separate package: NServiceBus.Metrics.PerformanceCounters",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public static void EnableCriticalTimePerformanceCounter(this EndpointConfiguration config)
        {
            throw new NotImplementedException();
        }
    }

    [ObsoleteEx(
        Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
        RemoveInVersion = "8",
        TreatAsErrorFromVersion = "7")]
    public class EncryptedValue
    {
        [ObsoleteEx(
            Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public string EncryptedBase64Value
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        [ObsoleteEx(
            Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public string Base64Iv
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
    }

    public partial class EndpointConfiguration
    {
        [ObsoleteEx(
            Message = "Use code-based configuration instead.",
            TreatAsErrorFromVersion = "7.0",
            RemoveInVersion = "8.0")]
        public void CustomConfigurationSource(IConfigurationSource configurationSource)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Use the AssemblyScanner configuration API.",
            ReplacementTypeOrMember = "AssemblyScannerConfigurationExtensions.AssemblyScanner",
            TreatAsErrorFromVersion = "7.0",
            RemoveInVersion = "8.0")]
        public void ExcludeAssemblies(params string[] assemblies)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Use the AssemblyScanner configuration API.",
            ReplacementTypeOrMember = "AssemblyScannerConfigurationExtensions.AssemblyScanner",
            TreatAsErrorFromVersion = "7.0",
            RemoveInVersion = "8.0")]
        public void ExcludeTypes(params Type[] types)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Use the AssemblyScanner configuration API.",
            ReplacementTypeOrMember = "AssemblyScannerConfigurationExtensions.AssemblyScanner",
            TreatAsErrorFromVersion = "7.0",
            RemoveInVersion = "8.0")]
        public void ScanAssembliesInNestedDirectories()
        {
            throw new NotImplementedException();
        }
    }

    public partial class FailedConfig
    {
        [ObsoleteEx(ReplacementTypeOrMember = "FailedConfig(string errorQueue, HashSet<Type> unrecoverableExceptionTypes)", RemoveInVersion = "8.0", TreatAsErrorFromVersion = "7.0")]
        public FailedConfig(string errorQueue)
        {
            throw new NotImplementedException();
        }
    }

    [ObsoleteEx(
        Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
        RemoveInVersion = "8",
        TreatAsErrorFromVersion = "7")]
    public interface IEncryptionService
    {
        [ObsoleteEx(
            Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        EncryptedValue Encrypt(string value, IOutgoingLogicalMessageContext context);

        [ObsoleteEx(
            Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        string Decrypt(EncryptedValue encryptedValue, IIncomingLogicalMessageContext context);
    }

    [ObsoleteEx(
       Message = "Json serialization is available as a dedicated 'NServiceBus.Newtonsoft.Json' package.",
       ReplacementTypeOrMember = "NServiceBus.NewtonsoftSerializer",
       RemoveInVersion = "8",
       TreatAsErrorFromVersion = "7")]
    public class JsonSerializer : SerializationDefinition
    {
        public override Func<IMessageMapper, IMessageSerializer> Configure(ReadOnlySettings settings)
        {
            throw new NotImplementedException();
        }
    }

    [ObsoleteEx(
        Message = "Json serialization is available as a dedicated 'NServiceBus.Newtonsoft.Json' package.",
        ReplacementTypeOrMember = "NServiceBus.NewtonsoftSerializer",
        RemoveInVersion = "8",
        TreatAsErrorFromVersion = "7")]
    public static class JsonSerializerConfigurationExtensions
    {
        [ObsoleteEx(
            Message = "Json serialization is available as a dedicated 'NServiceBus.Newtonsoft.Json' package.",
            ReplacementTypeOrMember = "NServiceBus.NewtonsoftSerializer",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public static void Encoding(this SerializationExtensions<JsonSerializer> config, Encoding encoding)
        {
            throw new NotImplementedException();
        }
    }

    public partial class RecoverabilitySettings
    {
        [ObsoleteEx(
            Message = "The legacy retries satellite was needed to migrate from V5 to V6, so it has been removed.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public RecoverabilitySettings DisableLegacyRetriesSatellite()
        {
            throw new NotImplementedException();
        }
    }

    public static partial class SettingsExtensions
    {
        [ObsoleteEx(
            Message = "Use code-based configuration instead.",
            TreatAsErrorFromVersion = "7.0",
            RemoveInVersion = "8.0")]
        public static T GetConfigSection<T>(this ReadOnlySettings settings) where T : class, new()
        {
            throw new NotImplementedException();
        }
    }

    [ObsoleteEx(
        Message = "Performance counters have been released as a separate package: NServiceBus.Metrics.PerformanceCounters",
        RemoveInVersion = "8",
        TreatAsErrorFromVersion = "7")]
    public static class SLAMonitoringConfig
    {
        [ObsoleteEx(
            Message = "Performance counters have been released as a separate package: NServiceBus.Metrics.PerformanceCounters",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public static void EnableSLAPerformanceCounter(this EndpointConfiguration config, TimeSpan sla)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Performance counters have been released as a separate package: NServiceBus.Metrics.PerformanceCounters",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public static void EnableSLAPerformanceCounter(this EndpointConfiguration config)
        {
            throw new NotImplementedException();
        }
    }

    [ObsoleteEx(
        Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
        ReplacementTypeOrMember = "NServiceBus.Encryption.MessageProperty.EncryptedString",
        RemoveInVersion = "8",
        TreatAsErrorFromVersion = "7")]
    public class WireEncryptedString
    {
        [ObsoleteEx(
            Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public WireEncryptedString()
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public WireEncryptedString(SerializationInfo info, StreamingContext context)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public string Value
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        [ObsoleteEx(
            Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public EncryptedValue EncryptedValue
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
    }

    public static partial class InstallConfigExtensions
    {
        [ObsoleteEx(
            Message = "Installers are now always disabled by default.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public static void DisableInstallers(this EndpointConfiguration config)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus.Config
{
    using System;

    [ObsoleteEx(
        Message = "Auditing can not be configured using a configuration file.",
        TreatAsErrorFromVersion = "7.0",
        RemoveInVersion = "8.0")]
    public class AuditConfig
    {
        [ObsoleteEx(
            Message = "Auditing can not be configured using a configuration file.",
            ReplacementTypeOrMember = "EndpointConfiguration.AuditProcessedMessagesTo",
            TreatAsErrorFromVersion = "7.0",
            RemoveInVersion = "8.0")]
        public string QueueName
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        [ObsoleteEx(
            Message = "Auditing can not be configured using a configuration file.",
            ReplacementTypeOrMember = "EndpointConfiguration.AuditProcessedMessagesTo",
            TreatAsErrorFromVersion = "7.0",
            RemoveInVersion = "8.0")]
        public TimeSpan OverrideTimeToBeReceived
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
    }

    [ObsoleteEx(
        Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
        RemoveInVersion = "8",
        TreatAsErrorFromVersion = "7")]
    public enum KeyFormat
    {
        Ascii = 0,
        Base64 = 1
    }

    [ObsoleteEx(
        Message = "Logging can not be configured using a configuration file.",
        ReplacementTypeOrMember = "LogManager.Use<DefaultFactory>()",
        TreatAsErrorFromVersion = "7",
        RemoveInVersion = "8")]
    public class Logging
    {
        [ObsoleteEx(
            Message = "Logging can not be configured using a configuration file.",
            ReplacementTypeOrMember = "LogManager.Use<DefaultFactory>().Level(LogLevel)",
            TreatAsErrorFromVersion = "7",
            RemoveInVersion = "8")]
        public string Threshold
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
    }

    [ObsoleteEx(
        Message = "Use code-based configuration instead.",
        TreatAsErrorFromVersion = "7.0",
        RemoveInVersion = "8.0")]
    public class MessageEndpointMapping : IComparable<MessageEndpointMapping>
    {
        [ObsoleteEx(
            Message = "Use code-based configuration instead.",
            TreatAsErrorFromVersion = "7.0",
            RemoveInVersion = "8.0")]
        public string Messages
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        [ObsoleteEx(
            Message = "Use code-based configuration instead.",
            TreatAsErrorFromVersion = "7.0",
            RemoveInVersion = "8.0")]
        public string Endpoint
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        [ObsoleteEx(
            Message = "Use code-based configuration instead.",
            TreatAsErrorFromVersion = "7.0",
            RemoveInVersion = "8.0")]
        public string AssemblyName
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        [ObsoleteEx(
            Message = "Use code-based configuration instead.",
            TreatAsErrorFromVersion = "7.0",
            RemoveInVersion = "8.0")]
        public string TypeFullName
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        [ObsoleteEx(
            Message = "Use code-based configuration instead.",
            TreatAsErrorFromVersion = "7.0",
            RemoveInVersion = "8.0")]
        public string Namespace
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        [ObsoleteEx(
            Message = "Use code-based configuration instead.",
            TreatAsErrorFromVersion = "7.0",
            RemoveInVersion = "8.0")]
        public int CompareTo(MessageEndpointMapping other)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Use code-based configuration instead.",
            TreatAsErrorFromVersion = "7.0",
            RemoveInVersion = "8.0")]
        public void Configure(Action<Type, string> mapTypeToEndpoint)
        {
            throw new NotImplementedException();
        }
    }

    [ObsoleteEx(
        Message = "Use code-based configuration instead.",
        TreatAsErrorFromVersion = "7.0",
        RemoveInVersion = "8.0")]
    public class MessageEndpointMappingCollection
    {
        [ObsoleteEx(
            Message = "Use code-based configuration instead.",
            TreatAsErrorFromVersion = "7.0",
            RemoveInVersion = "8.0")]
        public string AddElementName
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        [ObsoleteEx(
            Message = "Use code-based configuration instead.",
            TreatAsErrorFromVersion = "7.0",
            RemoveInVersion = "8.0")]
        public string ClearElementName
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        [ObsoleteEx(
            Message = "Use code-based configuration instead.",
            TreatAsErrorFromVersion = "7.0",
            RemoveInVersion = "8.0")]
        public string RemoveElementName => throw new NotImplementedException();

        [ObsoleteEx(
            Message = "Use code-based configuration instead.",
            TreatAsErrorFromVersion = "7.0",
            RemoveInVersion = "8.0")]
        public int Count => throw new NotImplementedException();

        [ObsoleteEx(
            Message = "Use code-based configuration instead.",
            TreatAsErrorFromVersion = "7.0",
            RemoveInVersion = "8.0")]
        public MessageEndpointMapping this[int index]
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        [ObsoleteEx(
            Message = "Use code-based configuration instead.",
            TreatAsErrorFromVersion = "7.0",
            RemoveInVersion = "8.0")]
        public MessageEndpointMapping this[string Name] => throw new NotImplementedException();

        [ObsoleteEx(
            Message = "Use code-based configuration instead.",
            TreatAsErrorFromVersion = "7.0",
            RemoveInVersion = "8.0")]
        public int IndexOf(MessageEndpointMapping mapping)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Use code-based configuration instead.",
            TreatAsErrorFromVersion = "7.0",
            RemoveInVersion = "8.0")]
        public void Add(MessageEndpointMapping mapping)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Use code-based configuration instead.",
            TreatAsErrorFromVersion = "7.0",
            RemoveInVersion = "8.0")]
        public void Remove(MessageEndpointMapping mapping)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Use code-based configuration instead.",
            TreatAsErrorFromVersion = "7.0",
            RemoveInVersion = "8.0")]
        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Use code-based configuration instead.",
            TreatAsErrorFromVersion = "7.0",
            RemoveInVersion = "8.0")]
        public void Remove(string name)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Use code-based configuration instead.",
            TreatAsErrorFromVersion = "7.0",
            RemoveInVersion = "8.0")]
        public void Clear()
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Use code-based configuration instead.",
            TreatAsErrorFromVersion = "7.0",
            RemoveInVersion = "8.0")]
        public bool IsReadOnly()
        {
            throw new NotImplementedException();
        }
    }

    [ObsoleteEx(
        Message = "Message forwarding for failed messages can not be configured using a configuration file.",
        ReplacementTypeOrMember = "EndpointConfiguration.SendFailedMessagesTo",
        TreatAsErrorFromVersion = "7",
        RemoveInVersion = "8")]
    public class MessageForwardingInCaseOfFaultConfig
    {
        [ObsoleteEx(
            Message = "Message forwarding for failed messages can not be configured using a configuration file.",
            ReplacementTypeOrMember = "EndpointConfiguration.SendFailedMessagesTo",
            TreatAsErrorFromVersion = "7",
            RemoveInVersion = "8")]
        public string ErrorQueue
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
    }

    [ObsoleteEx(
        Message = "MSMQ subscription storage can not be configured using a configuration file.",
        ReplacementTypeOrMember = "EndpointConfiguration.UsePersistence<MsmqPersistence>()",
        TreatAsErrorFromVersion = "7",
        RemoveInVersion = "8")]
    public class MsmqSubscriptionStorageConfig
    {
        [ObsoleteEx(
            Message = "MSMQ subscription storage can not be configured using a configuration file.",
            ReplacementTypeOrMember = "EndpointConfiguration.UsePersistence<MsmqPersistence>().SubscriptionQueue",
            TreatAsErrorFromVersion = "7",
            RemoveInVersion = "8")]
        public string Queue
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
    }

    [ObsoleteEx(
        Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
        RemoveInVersion = "8",
        TreatAsErrorFromVersion = "7")]
    public class RijndaelExpiredKey
    {
        [ObsoleteEx(
            Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public string Key
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        [ObsoleteEx(
            Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public string KeyIdentifier
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        [ObsoleteEx(
            Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public KeyFormat KeyFormat
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
    }

    [ObsoleteEx(
        Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
        RemoveInVersion = "8",
        TreatAsErrorFromVersion = "7")]
    public class RijndaelExpiredKeyCollection
    {
        [ObsoleteEx(
            Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public RijndaelExpiredKey this[int index]
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        [ObsoleteEx(
            Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public RijndaelExpiredKey this[string key] => throw new NotImplementedException();

        [ObsoleteEx(
            Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public int IndexOf(RijndaelExpiredKey encryptionKey)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public void Add(RijndaelExpiredKey mapping)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public void Remove(RijndaelExpiredKey mapping)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public void Remove(string name)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public void Clear()
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public bool IsReadOnly()
        {
            throw new NotImplementedException();
        }
    }

    [ObsoleteEx(
        Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
        RemoveInVersion = "8",
        TreatAsErrorFromVersion = "7")]
    public class RijndaelEncryptionServiceConfig
    {
        [ObsoleteEx(
            Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public string Key
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        [ObsoleteEx(
            Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public string KeyIdentifier
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        [ObsoleteEx(
            Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public RijndaelExpiredKeyCollection ExpiredKeys
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        [ObsoleteEx(
            Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public KeyFormat KeyFormat
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
    }

    [ObsoleteEx(
        Message = "The UnicastBus can not be configured using a configuration file.",
        TreatAsErrorFromVersion = "7.0",
        RemoveInVersion = "8.0")]
    public class UnicastBusConfig
    {
        [ObsoleteEx(
            Message = "UnicastBus time to be received for forwarded messages can not be configured using a configuration file.",
            TreatAsErrorFromVersion = "7.0",
            RemoveInVersion = "8.0")]
        public TimeSpan TimeToBeReceivedOnForwardedMessages
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        [ObsoleteEx(
            Message = "UnicastBus timeout manager can not be configured using a configuration file.",
            ReplacementTypeOrMember = "EndpointConfiguration.UseExternalTimeoutManager",
            TreatAsErrorFromVersion = "7.0",
            RemoveInVersion = "8.0")]
        public string TimeoutManagerAddress
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        [ObsoleteEx(
            Message = "UnicastBus message mapping can not be configured using a configuration file.",
            ReplacementTypeOrMember = "EndpointConfiguration.UseTransport<T>.Routing()",
            TreatAsErrorFromVersion = "7.0",
            RemoveInVersion = "8.0")]
        public MessageEndpointMappingCollection MessageEndpointMappings
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
    }
}

namespace NServiceBus.Config.ConfigurationSource
{
    [ObsoleteEx(
        Message = "Use code-based configuration instead of IConfigurationSource.",
        RemoveInVersion = "8.0",
        TreatAsErrorFromVersion = "7.0")]
    public interface IConfigurationSource
    {
        [ObsoleteEx(
            Message = "Use code-based configuration instead of IConfigurationSource.",
            RemoveInVersion = "8.0",
            TreatAsErrorFromVersion = "7.0")]
        T GetConfiguration<T>() where T : class, new();
    }

    [ObsoleteEx(
        Message = "Use code-based configuration instead of IProvideConfiguration.",
        RemoveInVersion = "8.0",
        TreatAsErrorFromVersion = "7.0")]
    public interface IProvideConfiguration<T>
    {
        [ObsoleteEx(
            Message = "Use code-based configuration instead of IProvideConfiguration.",
            RemoveInVersion = "8.0",
            TreatAsErrorFromVersion = "7.0")]
        T GetConfiguration();
    }
}

namespace NServiceBus.DeliveryConstraints
{
    using System;
    using Extensibility;

    public static partial class DeliveryConstraintContextExtensions
    {
        [ObsoleteEx(
            ReplacementTypeOrMember = "DeliveryConstraintContextExtensions.RemoveDeliveryConstraint",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public static void RemoveDeliveryConstaint(this ContextBag context, DeliveryConstraint constraint)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus.Features
{
    using System;
    using System.Threading.Tasks;
    using ObjectBuilder;
    using Transport;

    [ObsoleteEx(
        Message = "Performance counters have been released as a separate package: NServiceBus.Metrics.PerformanceCounters",
        RemoveInVersion = "8",
        TreatAsErrorFromVersion = "7")]
    public class CriticalTimeMonitoring { }

    public partial class FeatureConfigurationContext
    {
        [ObsoleteEx(
            Message = "The satellite's transaction mode needs to match the endpoint's transaction mode. As such the 'requiredTransportTransactionMode' parameter is redundant and should be removed.",
            RemoveInVersion = "8.0",
            TreatAsErrorFromVersion = "7.0",
            ReplacementTypeOrMember = "AddSatelliteReceiver(string name, string transportAddress, PushRuntimeSettings runtimeSettings, Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> recoverabilityPolicy, Func<IBuilder, MessageContext, Task> onMessage)")]
        public void AddSatelliteReceiver(string name, string transportAddress, TransportTransactionMode requiredTransportTransactionMode, PushRuntimeSettings runtimeSettings, Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> recoverabilityPolicy, Func<IBuilder, MessageContext, Task> onMessage)
        {
            throw new NotImplementedException();
        }
    }

    [ObsoleteEx(
        Message = "Performance counters have been released as a separate package: NServiceBus.Metrics.PerformanceCounters",
        RemoveInVersion = "8",
        TreatAsErrorFromVersion = "7")]
    public class ReceiveStatisticsPerformanceCounters { }

    [ObsoleteEx(
        Message = "Performance counters have been released as a separate package: NServiceBus.Metrics.PerformanceCounters",
        RemoveInVersion = "8",
        TreatAsErrorFromVersion = "7")]
    public class SLAMonitoring { }

}

namespace NServiceBus.Routing.Legacy
{
    using System;

    [ObsoleteEx(
        Message = "The distributor is no longer supported.",
        RemoveInVersion = "8",
        TreatAsErrorFromVersion = "7")]
    public static class ConfigureMSMQDistributor
    {
        [ObsoleteEx(
            Message = "The distributor is no longer supported.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public static void EnlistWithLegacyMSMQDistributor(this EndpointConfiguration config, string masterNodeAddress, string masterNodeControlAddress, int capacity)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus.Transport
{
    using System;

    public static partial class IncomingMessageExtensions
    {
        [ObsoleteEx(
            ReplacementTypeOrMember = "IncomingMessageExtensions.GetMessageIntent",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public static MessageIntentEnum GetMesssageIntent(this IncomingMessage message)
        {
            throw new NotImplementedException();
        }
    }

    public partial class TransportInfrastructure
    {
        [ObsoleteEx(
            RemoveInVersion = "8.0",
            TreatAsErrorFromVersion = "7.0",
            Message = "The outbox consent is no longer required. It is safe to ignore this property.")]
        public bool RequireOutboxConsent
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
    }
}

namespace NServiceBus.Pipeline
{
    public partial interface IInvokeHandlerContext
    {
        /// <summary>
        /// Indicates whether <see cref="IMessageHandlerContext.HandleCurrentMessageLater" /> has been called.
        /// </summary>
        [ObsoleteEx(
            Message = "HandleCurrentMessageLater has been deprecated.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        bool HandleCurrentMessageLaterWasCalled { get; }
    }
}

namespace NServiceBus
{
    using Features;

    // Just to make sure we remove it in v8. We keep it around for now just in case some external feature
    // depended on it using `DependsOn(string featureTypeName)`
    [ObsoleteEx(
           RemoveInVersion = "8",
           TreatAsErrorFromVersion = "7")]
    class Recoverability : Feature
    {
        public Recoverability()
        {
            EnableByDefault();
            DependsOnOptionally<DelayedDeliveryFeature>();

            Prerequisite(context => !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"),
                "Message recoverability is only relevant for endpoints receiving messages.");
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
        }
    }
}

namespace NServiceBus.Features
{
    // Just to make sure we remove it in v8. We keep it around for now just in case some external feature
    // depended on it using `DependsOn(string featureTypeName)` and also to set the host id default, see below.
    [ObsoleteEx(
           RemoveInVersion = "8",
           TreatAsErrorFromVersion = "7")]
    class HostInformationFeature : Feature
    {
        public HostInformationFeature()
        {
            EnableByDefault();

            // To allow users to avoid MD5 to be used by adding a hostid in a Feature default this have to stay here to maintain comaptibility.
            //For more details see the test: When_feature_overrides_hostid_from_feature_default
            Defaults(settings => settings.Get<HostingComponent.Configuration>().ApplyHostIdDefaultIfNeeded());
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
        }
    }
}

namespace NServiceBus
{
    using Features;

    // Just to make sure we remove it in v8. We keep it around for now just in case some external feature
    // depended on it using `DependsOn(string featureTypeName)`
    [ObsoleteEx(
           RemoveInVersion = "8",
           TreatAsErrorFromVersion = "7")]
    class HostStartupDiagnostics : Feature
    {
        public HostStartupDiagnostics()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
        }
    }
}

#pragma warning restore 1591