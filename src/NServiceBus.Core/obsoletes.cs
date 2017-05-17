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

    public partial class FailedConfig
    {
        [ObsoleteEx(ReplacementTypeOrMember = "FailedConfig(string errorQueue, HashSet<Type> unrecoverableExceptionTypes)", RemoveInVersion = "8.0", TreatAsErrorFromVersion = "7.0")]
        public FailedConfig(string errorQueue)
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