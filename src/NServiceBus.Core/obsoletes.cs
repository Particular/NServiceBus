// ReSharper disable UnusedTypeParameter
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedParameter.Global

#pragma warning disable 1591

namespace NServiceBus
{
    using System;

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

    public partial class FailedConfig
    {
        [ObsoleteEx(ReplacementTypeOrMember = "FailedConfig(string errorQueue, HashSet<Type> unrecoverableExceptionTypes)", RemoveInVersion = "8.0", TreatAsErrorFromVersion = "7.0")]
        public FailedConfig(string errorQueue)
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