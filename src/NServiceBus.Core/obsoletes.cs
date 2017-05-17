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
    using Pipeline;

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
}

namespace NServiceBus.Config
{
    using System;
    using System.Configuration;

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
        public ConfigurationElementCollectionType CollectionType => ConfigurationElementCollectionType.AddRemoveClearMap;

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
            throw new NotImplementedException(); ;
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
}

namespace NServiceBus.Features
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Transport;

    [ObsoleteEx(
        Message = "Performance counters have been released as a separate package: NServiceBus.Metrics.PerformanceCounters",
        RemoveInVersion = "8",
        TreatAsErrorFromVersion = "7")]
    public class CriticalTimeMonitoring : Feature
    {
        internal CriticalTimeMonitoring() { }

        [ObsoleteEx(
            Message = "Performance counters have been released as a separate package: NServiceBus.Metrics.PerformanceCounters",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            throw new NotImplementedException();
        }
    }

    public partial class FeatureConfigurationContext
    {
        [ObsoleteEx(
            Message = "The satellite's transaction mode needs to match the endpoint's transaction mode. As such the 'requiredTransportTransactionMode' parameter is redundant and should be removed.",
            RemoveInVersion = "8.0",
            TreatAsErrorFromVersion = "7.0",
            ReplacementTypeOrMember = AddSatelliteOverloadMemberDefinition)]
        public void AddSatelliteReceiver(string name, string transportAddress, TransportTransactionMode requiredTransportTransactionMode, PushRuntimeSettings runtimeSettings, Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> recoverabilityPolicy, Func<IBuilder, MessageContext, Task> onMessage)
        {
            throw new NotImplementedException();
        }
    }

    [ObsoleteEx(
        Message = "Performance counters have been released as a separate package: NServiceBus.Metrics.PerformanceCounters",
        RemoveInVersion = "8",
        TreatAsErrorFromVersion = "7")]
    public class ReceiveStatisticsPerformanceCounters : Feature
    {
        internal ReceiveStatisticsPerformanceCounters() { }

        [ObsoleteEx(
            Message = "Performance counters have been released as a separate package: NServiceBus.Metrics.PerformanceCounters",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            throw new NotImplementedException();
        }
    }

    [ObsoleteEx(
        Message = "Performance counters have been released as a separate package: NServiceBus.Metrics.PerformanceCounters",
        RemoveInVersion = "8",
        TreatAsErrorFromVersion = "7")]
    public class SLAMonitoring : Feature
    {
        internal SLAMonitoring()
        {
        }

        [ObsoleteEx(
            Message = "Performance counters have been released as a separate package: NServiceBus.Metrics.PerformanceCounters",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus.Transport
{
    public partial class TransportInfrastructure
    {
        [ObsoleteEx(
            RemoveInVersion = "8.0",
            TreatAsErrorFromVersion = "7.0",
            Message = "The outbox consent is no longer required. It is safe to ignore this property.")]
        public bool RequireOutboxConsent { get; protected set; }
    }
}

#pragma warning restore 1591