// ReSharper disable UnusedTypeParameter
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedParameter.Global

#pragma warning disable 1591

using System;

namespace NServiceBus.Gateway.Deduplication
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;

    [ObsoleteEx(
        Message = "Gateway persistence has been moved to the NServiceBus.Gateway dedicated package.",
        RemoveInVersion = "9.0.0",
        TreatAsErrorFromVersion = "8.0.0")]
    public interface IDeduplicateMessages
    {
        Task<bool> DeduplicateMessage(string clientId, DateTime timeReceived, ContextBag context);
    }
}

namespace NServiceBus
{
    using System;

    public static partial class Headers
    {
        [ObsoleteEx(
            RemoveInVersion = "9.0",
            TreatAsErrorFromVersion = "8.0",
            Message = "Not intended for public usage.")]
        public const string HeaderName = "Header";
    }

    [ObsoleteEx(
        Message = "Gateway persistence has been moved to the NServiceBus.Gateway dedicated package.",
        RemoveInVersion = "9.0.0",
        TreatAsErrorFromVersion = "8.0.0")]
    public static class InMemoryGatewayPersistenceConfigurationExtensions
    {
        [ObsoleteEx(
            Message = "Gateway persistence has been moved to the NServiceBus.Gateway dedicated package.",
            RemoveInVersion = "9.0.0",
            TreatAsErrorFromVersion = "8.0.0")]
        public static void GatewayDeduplicationCacheSize(this PersistenceExtensions<InMemoryPersistence> persistenceExtensions, int maxSize)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus.Features
{
    [ObsoleteEx(
        Message = "Gateway persistence has been moved to the NServiceBus.Gateway dedicated package.",
        RemoveInVersion = "9.0.0",
        TreatAsErrorFromVersion = "8.0.0")]
    public class InMemoryGatewayPersistence : Feature
    {
        internal InMemoryGatewayPersistence()
        {
            throw new NotImplementedException();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            throw new NotImplementedException();
        }
    }

    [ObsoleteEx(
        Message = "Use 'TransportExtensions<T>.DisablePublishing()' to avoid the need for a subscription storage if this endpoint does not publish events.",
        RemoveInVersion = "9",
        TreatAsErrorFromVersion = "8")]
    public class MessageDrivenSubscriptions : Feature
    {
        internal MessageDrivenSubscriptions()
        {
            throw new NotImplementedException();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus
{
    public abstract partial class StorageType
    {
        [ObsoleteEx(
            Message = "Gateway persistence has been moved to the NServiceBus.Gateway dedicated package.",
            RemoveInVersion = "9.0.0",
            TreatAsErrorFromVersion = "8.0.0")]
        public sealed class GatewayDeduplication : StorageType
        {
            internal GatewayDeduplication() : base("GatewayDeduplication")
            {
                throw new NotImplementedException();
            }
        }
    }
}

namespace NServiceBus.Faults
{
    using System;

    public partial class ErrorsNotifications
    {
#pragma warning disable 67

        [ObsoleteEx(
            Message = "The .NET event based error notifications will be deprecated in favor of Task-based callbacks. Use endpointConfiguration.Recoverability().Failed(settings => settings.OnMessageSentToErrorQueue(callback)) instead.",
            RemoveInVersion = "9.0",
            TreatAsErrorFromVersion = "8.0")]
        public event EventHandler<FailedMessage> MessageSentToErrorQueue;

        [ObsoleteEx(
            Message = "The .NET event based error notifications will be deprecated in favor of Task-based callbacks. Use endpointConfiguration.Recoverability().Immediate(settings => settings.OnMessageBeingRetried(callback)) instead.",
            RemoveInVersion = "9.0",
            TreatAsErrorFromVersion = "8.0")]
        public event EventHandler<ImmediateRetryMessage> MessageHasFailedAnImmediateRetryAttempt;

        [ObsoleteEx(
            Message = "The .NET event based error notifications will be deprecated in favor of Task-based callbacks. Use endpointConfiguration.Recoverability().Delayed(settings => settings.OnMessageBeingRetried(callback)) instead.",
            RemoveInVersion = "9.0",
            TreatAsErrorFromVersion = "8.0")]
        public event EventHandler<DelayedRetryMessage> MessageHasBeenSentToDelayedRetries;

#pragma warning restore 67
    }
}

namespace NServiceBus.Settings
{
    public partial class SettingsHolder
    {
        [ObsoleteEx(
            ReplacementTypeOrMember = "Set<T>(T value)",
            TreatAsErrorFromVersion = "8.0",
            RemoveInVersion = "9.0")]
        public void Set<T>(object value)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            ReplacementTypeOrMember = "SetDefault<T>(T value)",
            TreatAsErrorFromVersion = "8.0",
            RemoveInVersion = "9.0")]
        public void SetDefault<T>(object value)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus
{
    using System;

    public partial class TransportExtensions<T>
    {
        [ObsoleteEx(
            Message = "Loading named connection strings is no longer supported",
            ReplacementTypeOrMember = "TransportExtensions<T>.ConnectionString(connectionString)",
            TreatAsErrorFromVersion = "7.0",
            RemoveInVersion = "9.0")]
        public new TransportExtensions<T> ConnectionStringName(string name)
        {
            throw new NotImplementedException();
        }
    }

    public partial class TransportExtensions
    {
        [ObsoleteEx(
            Message = "The ability to used named connection strings has been removed. Instead, load the connection string in your code and pass the value to TransportExtensions.ConnectionString(connectionString)",
            ReplacementTypeOrMember = "TransportExtensions.ConnectionString(connectionString)",
            TreatAsErrorFromVersion = "7.0",
            RemoveInVersion = "9.0")]
        public TransportExtensions ConnectionStringName(string name)
        {
            throw new NotImplementedException();
        }
    }
}

#pragma warning restore 1591