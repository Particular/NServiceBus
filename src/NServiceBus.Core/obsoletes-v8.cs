#pragma warning disable 1591
#pragma warning disable PS0013 // A Func used as a method parameter with a Task, ValueTask, or ValueTask<T> return type argument should have at least one CancellationToken parameter type argument unless it has a parameter type argument implementing ICancellableContext
#pragma warning disable PS0018 // A task-returning method should have a CancellationToken parameter unless it has a parameter implementing ICancellableContext

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
    using Container;
    using ObjectBuilder.Common;
    using Settings;

    public partial class EndpointConfiguration
    {
        [ObsoleteEx(
            Message = "Use the externally managed container mode to integrate with third party dependency injection containers.",
            RemoveInVersion = "9.0",
            TreatAsErrorFromVersion = "8.0")]
        public void UseContainer<T>(Action<ContainerCustomizations> customizations = null) where T : ContainerDefinition, new()
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Use the externally managed container mode to integrate with third party dependency injection containers.",
            RemoveInVersion = "9.0",
            TreatAsErrorFromVersion = "8.0")]
        public void UseContainer(Type definitionType)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Use the externally managed container mode to integrate with third party dependency injection containers.",
            RemoveInVersion = "9.0",
            TreatAsErrorFromVersion = "8.0")]
        public void UseContainer(IContainer builder)
        {
            throw new NotImplementedException();
        }
    }

    [ObsoleteEx(
        RemoveInVersion = "9",
        TreatAsErrorFromVersion = "8",
        ReplacementTypeOrMember = "EndpointConfiguration.UseTransport(TransportDefinition)")]
    public static class UseTransportExtensions
    {
        [ObsoleteEx(
            RemoveInVersion = "9",
            TreatAsErrorFromVersion = "8",
            ReplacementTypeOrMember = "EndpointConfiguration.UseTransport(TransportDefinition)")]
        public static TransportExtensions UseTransport(this EndpointConfiguration endpointConfiguration, Type transportDefinitionType)
        {
            throw new NotImplementedException();
        }
    }

    public static partial class Headers
    {
        [ObsoleteEx(
            RemoveInVersion = "9.0",
            TreatAsErrorFromVersion = "8.0",
            Message = "Not intended for public usage.")]
        public const string HeaderName = "Header";

        [ObsoleteEx(
            Message = "Non-durable delivery support has been moved to the transports that can support it. See the upgrade guide for more details.",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        public const string NonDurableMessage = "NServiceBus.NonDurableMessage";
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

    [ObsoleteEx(
        Message = "Transport infrastructure setup control is not based on the installer configuration.",
        RemoveInVersion = "9.0.0",
        TreatAsErrorFromVersion = "8.0.0")]
    public static class ConfigureQueueCreation
    {
        [ObsoleteEx(
            Message = "Transport infrastructure setup control is not based on the installer configuration.",
            RemoveInVersion = "9.0.0",
            TreatAsErrorFromVersion = "8.0.0")]
        public static void DoNotCreateQueues(this EndpointConfiguration config)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Transport infrastructure setup control is not based on the installer configuration.",
            RemoveInVersion = "9.0.0",
            TreatAsErrorFromVersion = "8.0.0")]
        public static bool CreateQueues(this ReadOnlySettings settings)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus.Container
{
    [ObsoleteEx(
        Message = "The NServiceBus dependency injection container API has been deprecated. Use the externally managed container mode to use custom containers.",
        RemoveInVersion = "9.0.0",
        TreatAsErrorFromVersion = "8.0.0")]
    public abstract class ContainerDefinition
    {
    }

    [ObsoleteEx(
        Message = "The NServiceBus dependency injection container API has been deprecated. Use the externally managed container mode to use custom containers.",
        RemoveInVersion = "9.0.0",
        TreatAsErrorFromVersion = "8.0.0")]
    public class ContainerCustomizations
    {
        ContainerCustomizations()
        {
            // private ctor
        }
    }
}

namespace NServiceBus.ObjectBuilder
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.DependencyInjection;

    [ObsoleteEx(
        ReplacementTypeOrMember = nameof(IServiceProvider),
        TreatAsErrorFromVersion = "8.0.0",
        RemoveInVersion = "9.0.0")]
    public interface IBuilder : IDisposable
    {
        [ObsoleteEx(Message = "The Build method is not supported anymore.", ReplacementTypeOrMember = nameof(IServiceProvider.GetService), TreatAsErrorFromVersion = "8.0.0", RemoveInVersion = "9.0.0")]
        object Build(Type typeToBuild);

        [ObsoleteEx(Message = "The CreateChildBuilder method is not supported anymore.", ReplacementTypeOrMember = nameof(ServiceProviderServiceExtensions.CreateScope), TreatAsErrorFromVersion = "8.0.0", RemoveInVersion = "9.0.0")]
        IBuilder CreateChildBuilder();

        [ObsoleteEx(Message = "The Build<T> method is not supported anymore.", ReplacementTypeOrMember = nameof(ServiceProviderServiceExtensions.GetService), TreatAsErrorFromVersion = "8.0.0", RemoveInVersion = "9.0.0")]
        T Build<T>();

        [ObsoleteEx(Message = "The BuildAll<T> method is not supported anymore.", ReplacementTypeOrMember = nameof(ServiceProviderServiceExtensions.GetServices), TreatAsErrorFromVersion = "8.0.0", RemoveInVersion = "9.0.0")]
        IEnumerable<T> BuildAll<T>();

        [ObsoleteEx(Message = "The BuildAll method is not supported anymore.", ReplacementTypeOrMember = nameof(ServiceProviderServiceExtensions.GetServices), TreatAsErrorFromVersion = "8.0.0", RemoveInVersion = "9.0.0")]
        IEnumerable<object> BuildAll(Type typeToBuild);

        [ObsoleteEx(Message = "The Release method is not supported anymore.", TreatAsErrorFromVersion = "8.0.0", RemoveInVersion = "9.0.0")]
        void Release(object instance);

        [ObsoleteEx(Message = "The BuildAndDispatch method is not supported anymore.", TreatAsErrorFromVersion = "8.0.0", RemoveInVersion = "9.0.0")]
        void BuildAndDispatch(Type typeToBuild, Action<object> action);
    }

    [ObsoleteEx(
        ReplacementTypeOrMember = nameof(IServiceCollection),
        TreatAsErrorFromVersion = "8.0",
        RemoveInVersion = "9.0")]
    public interface IConfigureComponents
    {
        [ObsoleteEx(
            ReplacementTypeOrMember = "IServiceCollection.ConfigureComponent",
            TreatAsErrorFromVersion = "8.0",
            RemoveInVersion = "9.0")]
        void ConfigureComponent(Type concreteComponent, DependencyLifecycle dependencyLifecycle);

        [ObsoleteEx(
            ReplacementTypeOrMember = "IServiceCollection.ConfigureComponent",
            TreatAsErrorFromVersion = "8.0",
            RemoveInVersion = "9.0")]
        void ConfigureComponent<T>(DependencyLifecycle dependencyLifecycle);

        [ObsoleteEx(
            ReplacementTypeOrMember = "IServiceCollection.ConfigureComponent",
            TreatAsErrorFromVersion = "8.0",
            RemoveInVersion = "9.0")]
        void ConfigureComponent<T>(Func<T> componentFactory, DependencyLifecycle dependencyLifecycle);

        [ObsoleteEx(
            ReplacementTypeOrMember = "IServiceCollection.ConfigureComponent",
            TreatAsErrorFromVersion = "8.0",
            RemoveInVersion = "9.0")]
        void ConfigureComponent<T>(Func<IServiceProvider, T> componentFactory, DependencyLifecycle dependencyLifecycle);

        [ObsoleteEx(
            ReplacementTypeOrMember = "IServiceCollection.AddSingleton",
            TreatAsErrorFromVersion = "8.0",
            RemoveInVersion = "9.0")]
        void RegisterSingleton(Type lookupType, object instance);

        [ObsoleteEx(
            ReplacementTypeOrMember = "IServiceCollection.AddSingleton",
            TreatAsErrorFromVersion = "8.0",
            RemoveInVersion = "9.0")]
        void RegisterSingleton<T>(T instance);

        [ObsoleteEx(
            ReplacementTypeOrMember = "IServiceCollection.HasComponent",
            TreatAsErrorFromVersion = "8.0",
            RemoveInVersion = "9.0")]
        bool HasComponent<T>();

        [ObsoleteEx(
            ReplacementTypeOrMember = "IServiceCollection.HasComponent",
            TreatAsErrorFromVersion = "8.0",
            RemoveInVersion = "9.0")]
        bool HasComponent(Type componentType);
    }
}

namespace NServiceBus.ObjectBuilder.Common
{
    using System;

    [ObsoleteEx(
        Message = "The NServiceBus dependency injection container API has been deprecated. Use the externally managed container mode to use custom containers.",
        RemoveInVersion = "9.0.0",
        TreatAsErrorFromVersion = "8.0.0")]
    public interface IContainer : IDisposable
    {
    }
}

namespace NServiceBus.Features
{
    using System;

    [ObsoleteEx(
        Message = "Gateway persistence has been moved to the NServiceBus.Gateway dedicated package.",
        RemoveInVersion = "9.0.0",
        TreatAsErrorFromVersion = "8.0.0")]
    public class InMemoryGatewayPersistence
    {
        internal InMemoryGatewayPersistence() => throw new NotImplementedException();
    }
}

namespace NServiceBus
{
    using System;

    public abstract partial class StorageType
    {
        [ObsoleteEx(
            Message = "Gateway persistence has been moved to the NServiceBus.Gateway dedicated package.",
            RemoveInVersion = "9.0.0",
            TreatAsErrorFromVersion = "8.0.0")]
        public sealed class GatewayDeduplication
        {
            internal GatewayDeduplication() => throw new NotImplementedException();
        }
    }
}

namespace NServiceBus.Faults
{
    using System;

    public class ErrorsNotifications
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
    using System;

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
    using Configuration.AdvancedExtensibility;
    using Settings;
    using Transport;

    // The type itself can't be configured with TreatAsErrorFromVersion 8 as downstream extension methods require the type to obsolete their own extension methods.
    [ObsoleteEx(
        Message = "Configure the transport via the TransportDefinition instance's properties",
        TreatAsErrorFromVersion = "9.0",
        RemoveInVersion = "9.0")]
    public class TransportExtensions<T> : TransportExtensions where T : TransportDefinition
    {
        [ObsoleteEx(
            Message = "Configure the transport via the TransportDefinition instance's properties",
            TreatAsErrorFromVersion = "8.0",
            RemoveInVersion = "9.0")]
        public TransportExtensions(SettingsHolder settings) : base(settings)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Loading named connection strings is no longer supported",
            ReplacementTypeOrMember = "TransportExtensions<T>.ConnectionString(connectionString)",
            TreatAsErrorFromVersion = "8.0",
            RemoveInVersion = "9.0")]
        public new TransportExtensions<T> ConnectionStringName(string name)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Setting connection string at the endpoint level is no longer supported. Transport specific configuration options should be used instead",
            TreatAsErrorFromVersion = "8.0",
            RemoveInVersion = "9.0")]
        public new TransportExtensions<T> ConnectionString(string connectionString)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Setting connection string at the endpoint level is no longer supported. Transport specific configuration options should be used instead",
            TreatAsErrorFromVersion = "8.0",
            RemoveInVersion = "9.0")]
        public new TransportExtensions<T> ConnectionString(Func<string> connectionString)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            TreatAsErrorFromVersion = "8.0",
            RemoveInVersion = "9.0",
            ReplacementTypeOrMember = "TransportDefinition.TransportTransactionMode")]
        public new TransportExtensions<T> Transactions(TransportTransactionMode transportTransactionMode)
        {
            throw new NotImplementedException();
        }
    }

    // The type itself can't be configured with TreatAsErrorFromVersion 8 as downstream extension methods require the type to obsolete their own extension methods.
    [ObsoleteEx(
        Message = "Configure the transport via the TransportDefinition instance's properties",
        TreatAsErrorFromVersion = "9.0",
        RemoveInVersion = "9.0")]
    public class TransportExtensions : ExposeSettings
    {
        [ObsoleteEx(
            Message = "Configure the transport via the TransportDefinition instance's properties",
            TreatAsErrorFromVersion = "8.0",
            RemoveInVersion = "9.0")]
        public TransportExtensions(SettingsHolder settings)
            : base(settings)
        {
        }

        [ObsoleteEx(
            Message = "The ability to used named connection strings has been removed. Instead, load the connection string in your code and pass the value to TransportExtensions.ConnectionString(connectionString)",
            ReplacementTypeOrMember = "TransportExtensions.ConnectionString(connectionString)",
            TreatAsErrorFromVersion = "7.0",
            RemoveInVersion = "9.0")]
        public TransportExtensions ConnectionStringName(string name)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Setting connection string at the endpoint level is no longer supported. Transport specific configuration options should be used instead",
            TreatAsErrorFromVersion = "8.0",
            RemoveInVersion = "9.0")]
        public TransportExtensions ConnectionString(string connectionString)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Setting connection string at the endpoint level is no longer supported. Transport specific configuration options should be used instead",
            TreatAsErrorFromVersion = "8.0",
            RemoveInVersion = "9.0")]
        public TransportExtensions ConnectionString(Func<string> connectionString)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            TreatAsErrorFromVersion = "8.0",
            RemoveInVersion = "9.0",
            ReplacementTypeOrMember = "TransportDefinition.TransportTransactionMode")]
        public TransportExtensions Transactions(TransportTransactionMode transportTransactionMode)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus
{
    using System;

    [ObsoleteEx(TreatAsErrorFromVersion = "8", RemoveInVersion = "9")]
    public static class ConfigureForwarding
    {
        [ObsoleteEx(
            Message = "Message forwarding is no longer supported natively by NServiceBus. For auditing messages, use endpointConfiguration.AuditProcessedMessagesTo(address). If true message forwarding capabilities are needed, use a custom pipeline behavior, an example of which can be found in the documentation.",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        public static void ForwardReceivedMessagesTo(this EndpointConfiguration config, string address)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus.Features
{
    using System;

    [ObsoleteEx(
        Message = "Message forwarding is no longer supported, but can be implemented as a custom pipeline behavior.",
        TreatAsErrorFromVersion = "8",
        RemoveInVersion = "9")]
    public class ForwardReceivedMessages
    {
        internal ForwardReceivedMessages() => throw new NotImplementedException();
    }
}

namespace NServiceBus.Pipeline
{
    using System;

    [ObsoleteEx(
        Message = "Message forwarding is no longer supported, but can be implemented as a custom pipeline behavior.",
        TreatAsErrorFromVersion = "8",
        RemoveInVersion = "9")]
    public interface IForwardingContext : IBehaviorContext
    {
    }

    public partial class PipelineSettings
    {
        [ObsoleteEx(
            Message = "Removing behaviors from the pipeline is discouraged, to disable a behavior replace the behavior by an empty one. Documentation: https://docs.particular.net/nservicebus/pipeline/manipulate-with-behaviors ",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        public void Remove(string stepId)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus
{
    using System;
    using Pipeline;
    using Transport;

    public static partial class ConnectorContextExtensions
    {
        [ObsoleteEx(
        Message = "Message forwarding is no longer supported, but can be implemented as a custom pipeline behavior.",
        TreatAsErrorFromVersion = "8",
        RemoveInVersion = "9")]
        public static IForwardingContext CreateForwardingContext(this ForkConnector<IIncomingPhysicalMessageContext, IForwardingContext> forwardingContext, OutgoingMessage message, string forwardingAddress, IIncomingPhysicalMessageContext sourceContext)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus.Unicast
{
    using System;

    [ObsoleteEx(
        TreatAsErrorFromVersion = "8",
        RemoveInVersion = "9")]
    public static class BuilderExtensions
    {
        [ObsoleteEx(
            Message = "Replace usages of ForEach<T> with a foreach loop",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        public static void ForEach<T>(this IServiceProvider builder, Action<T> action)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus
{
    using System;

    [ObsoleteEx(
            Message = "The built-in scheduler is no longer supported, see our upgrade guide for details on how to migrate to plain .NET Timers",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
    public class ScheduledTask
    {
        internal ScheduledTask() => throw new NotImplementedException();
    }
}

namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;


    [ObsoleteEx(
            Message = "The built-in scheduler is no longer supported, see our upgrade guide for details on how to migrate to plain .NET Timers",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
    public static class ScheduleExtensions
    {
        public static Task ScheduleEvery(this IMessageSession session, TimeSpan timeSpan, Func<IPipelineContext, Task> task)
        {
            throw new NotImplementedException();
        }

        public static Task ScheduleEvery(this IMessageSession session, TimeSpan timeSpan, string name, Func<IPipelineContext, Task> task)
        {
            throw new NotImplementedException();

        }
    }
}

namespace NServiceBus.Features
{
    [ObsoleteEx(
            Message = "The built-in scheduler is no longer supported, see our upgrade guide for details on how to migrate to plain .NET Timers",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
    public class Scheduler
    {
    }
}

namespace NServiceBus
{
    using System;
    using Outbox;
    using Persistence;

    [ObsoleteEx(Message = "The InMemoryPersistence has been moved to a dedicated Nuget Package called NServiceBus.Persistence.NonDurable and has been renamed to NonDurablePersistence", TreatAsErrorFromVersion = "8.0.0", RemoveInVersion = "9.0.0")]
    public class InMemoryPersistence : PersistenceDefinition
    {
    }

    [ObsoleteEx(Message = "The InMemoryPersistence has been moved to a dedicated Nuget Package called NServiceBus.Persistence.NonDurable and has been renamed to NonDurablePersistence", TreatAsErrorFromVersion = "8.0.0", RemoveInVersion = "9.0.0")]
    public class InMemoryTimeoutPersistence
    {
    }

    [ObsoleteEx(Message = "The InMemoryPersistence has been moved to a dedicated Nuget Package called NServiceBus.Persistence.NonDurable and has been renamed to NonDurablePersistence", TreatAsErrorFromVersion = "8.0.0", RemoveInVersion = "9.0.0")]
    public class InMemorySubscriptionPersistence
    {
    }

    [ObsoleteEx(Message = "The InMemoryPersistence has been moved to a dedicated Nuget Package called NServiceBus.Persistence.NonDurable and has been renamed to NonDurablePersistence", TreatAsErrorFromVersion = "8.0.0", RemoveInVersion = "9.0.0")]
    public class InMemorySagaPersistence
    {
    }

    [ObsoleteEx(Message = "The InMemoryPersistence has been moved to a dedicated Nuget Package called NServiceBus.Persistence.NonDurable and has been renamed to NonDurablePersistence", TreatAsErrorFromVersion = "8.0.0", RemoveInVersion = "9.0.0")]
    public static class InMemoryOutboxSettingsExtensions
    {
        [ObsoleteEx(Message = "The InMemoryPersistence has been moved to a dedicated Nuget Package called NServiceBus.Persistence.NonDurable and has been renamed to NonDurablePersistence", TreatAsErrorFromVersion = "8.0.0", RemoveInVersion = "9.0.0")]
        public static OutboxSettings TimeToKeepDeduplicationData(this OutboxSettings settings, TimeSpan time)
        {
            throw new NotSupportedException();
        }
    }

    [ObsoleteEx(Message = "The InMemoryPersistence has been moved to a dedicated Nuget Package called NServiceBus.Persistence.NonDurable and has been renamed to NonDurablePersistence", TreatAsErrorFromVersion = "8.0.0", RemoveInVersion = "9.0.0")]
    public class InMemoryOutboxPersistence
    {
    }
}

namespace NServiceBus
{
    [ObsoleteEx(
        Message = "Non-durable delivery support has been moved to the transports that can support it. See the upgrade guide for more details.",
        TreatAsErrorFromVersion = "8",
        RemoveInVersion = "9")]
    public class NonDurableDelivery
    {
    }
}

namespace NServiceBus
{
    using System;
    using Settings;

    [ObsoleteEx(
        Message = "Non-durable delivery support has been moved to the transports that can support it. See the upgrade guide for more details.",
        TreatAsErrorFromVersion = "8",
        RemoveInVersion = "9")]
    public static class DurableMessagesConfig
    {
        [ObsoleteEx(
            Message = "Non-durable delivery support has been moved to the transports that can support it. See the upgrade guide for more details.",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        public static void EnableDurableMessages(this EndpointConfiguration config)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Non-durable delivery support has been moved to the transports that can support it. See the upgrade guide for more details.",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        public static void DisableDurableMessages(this EndpointConfiguration config)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Non-durable delivery support has been moved to the transports that can support it. See the upgrade guide for more details.",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        public static bool DurableMessagesEnabled(this ReadOnlySettings settings)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus
{
    using System;

    [ObsoleteEx(
        Message = "Non-durable delivery support has been moved to the transports that can support it. See the upgrade guide for more details.",
        TreatAsErrorFromVersion = "8",
        RemoveInVersion = "9")]
    public static class DurableMessagesConventionExtensions
    {
        [ObsoleteEx(
            Message = "Non-durable delivery support has been moved to the transports that can support it. See the upgrade guide for more details.",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        public static ConventionsBuilder DefiningExpressMessagesAs(this ConventionsBuilder builder, Func<Type, bool> definesExpressMessageType)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus
{
    using System;

    [ObsoleteEx(
        Message = "Non-durable delivery support has been moved to the transports that can support it. See the upgrade guide for more details.",
        TreatAsErrorFromVersion = "8",
        RemoveInVersion = "9")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public sealed class ExpressAttribute : Attribute
    {
    }
}

namespace NServiceBus
{
    using System;

    [ObsoleteEx(
        Message = "Public APIs no longer use DateTime but DateTimeOffset. See the upgrade guide for more details.",
        ReplacementTypeOrMember = "NServiceBus.DateTimeOffsetExtensions",
        TreatAsErrorFromVersion = "8",
        RemoveInVersion = "9")]
    public static class DateTimeExtensions
    {
        [ObsoleteEx(
            Message = "Public APIs no longer use DateTime but DateTimeOffset. See the upgrade guide for more details.",
            ReplacementTypeOrMember = "NServiceBus.DateTimeOffsetHelper.ToWireFormattedString",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        public static string ToWireFormattedString(DateTime dateTime)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Public APIs no longer use DateTime but DateTimeOffset. See the upgrade guide for more details.",
            ReplacementTypeOrMember = "NServiceBus.DateTimeOffsetHelper.ToDateTimeOffset",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        public static DateTime ToUtcDateTime(string wireFormattedString)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus
{
    using System;

    public abstract partial class StorageType
    {
        [ObsoleteEx(
            Message = "The timeout manager has been removed in favor of native delayed delivery support provided by transports. See the upgrade guide for more details.",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        public sealed class Timeouts
        {
            internal Timeouts()
            {
                throw new NotImplementedException();
            }
        }
    }

    public class TimeoutManagerConfiguration
    {
        [ObsoleteEx(
            Message = "The timeout manager has been removed in favor of native delayed delivery support provided by transports. See the upgrade guide for more details.",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        internal TimeoutManagerConfiguration() => throw new NotImplementedException();
    }

    [ObsoleteEx(
        Message = "The timeout manager has been removed. See the upgrade guide for more details",
        TreatAsErrorFromVersion = "8",
        RemoveInVersion = "9")]
    public static class TimeoutManagerConfigurationExtensions
    {
        [ObsoleteEx(
            Message = "The timeout manager has been removed in favor of native delayed delivery support provided by transports. See the upgrade guide for more details.",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        public static TimeoutManagerConfiguration TimeoutManager(this EndpointConfiguration endpointConfiguration)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "The timeout manager has been removed in favor of native delayed delivery support provided by transports. See the upgrade guide for more details.",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        public static void LimitMessageProcessingConcurrencyTo(this TimeoutManagerConfiguration timeoutManagerConfiguration, int maxConcurrency)
        {
            throw new NotImplementedException();
        }
    }

    [ObsoleteEx(
        Message = "The timeout manager has been removed in favor of native delayed delivery support provided by transports. See the upgrade guide for more details.",
        TreatAsErrorFromVersion = "8",
        RemoveInVersion = "9")]
    public static class ConfigurationTimeoutExtensions
    {
        [ObsoleteEx(
            Message = "The timeout manager has been removed. See the upgrade guide for more details",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        public static void TimeToWaitBeforeTriggeringCriticalErrorOnTimeoutOutages(this EndpointConfiguration config, TimeSpan timeToWait)
        {
            throw new NotImplementedException();
        }
    }

}

namespace NServiceBus.Features
{
    using System;

    [ObsoleteEx(
        Message = "The timeout manager has been removed in favor of native delayed delivery support provided by transports. See the upgrade guide for more details.",
        TreatAsErrorFromVersion = "8",
        RemoveInVersion = "9")]
    public class TimeoutManager
    {
        internal TimeoutManager() => throw new NotImplementedException();
    }
}

namespace NServiceBus.Timeout.Core
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Extensibility;

    [ObsoleteEx(
        Message = "The timeout manager has been removed in favor of native delayed delivery support provided by transports. See the upgrade guide for more details.",
        TreatAsErrorFromVersion = "8",
        RemoveInVersion = "9")]
    public interface IPersistTimeouts
    {
        Task Add(TimeoutData timeout, ContextBag context);

        Task<bool> TryRemove(string timeoutId, ContextBag context);

        Task<TimeoutData> Peek(string timeoutId, ContextBag context);

        Task RemoveTimeoutBy(Guid sagaId, ContextBag context);
    }

    [ObsoleteEx(
        Message = "The timeout manager has been removed in favor of native delayed delivery support provided by transports. See the upgrade guide for more details.",
        TreatAsErrorFromVersion = "8",
        RemoveInVersion = "9")]
    public interface IQueryTimeouts
    {
        Task<TimeoutsChunk> GetNextChunk(DateTime startSlice);
    }

    [ObsoleteEx(
        Message = "The timeout manager has been removed in favor of native delayed delivery support provided by transports. See the upgrade guide for more details.",
        TreatAsErrorFromVersion = "8",
        RemoveInVersion = "9")]
    public class TimeoutData
    {
        public string Id => throw new NotImplementedException();

        public string Destination => throw new NotImplementedException();

        public Guid SagaId => throw new NotImplementedException();

        public byte[] State => throw new NotImplementedException();

        public DateTime Time => throw new NotImplementedException();

        public string OwningTimeoutManager => throw new NotImplementedException();

        public Dictionary<string, string> Headers => throw new NotImplementedException();
    }

    [ObsoleteEx(
        Message = "The timeout manager has been removed in favor of native delayed delivery support provided by transports. See the upgrade guide for more details.",
        TreatAsErrorFromVersion = "8",
        RemoveInVersion = "9")]
    public class TimeoutsChunk
    {
        public TimeoutsChunk(Timeout[] dueTimeouts, DateTime nextTimeToQuery)
        {
            throw new NotImplementedException();
        }

        public Timeout[] DueTimeouts => throw new NotImplementedException();

        public DateTime NextTimeToQuery => throw new NotImplementedException();

        [ObsoleteEx(
            Message = "The timeout manager has been removed in favor of native delayed delivery support provided by transports. See the upgrade guide for more details.",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        public struct Timeout
        {
            public Timeout(string id, DateTime dueTime)
            {
                throw new NotImplementedException();
            }

            public string Id => throw new NotImplementedException();

            public DateTime DueTime => throw new NotImplementedException();
        }
    }
}

namespace NServiceBus.DelayedDelivery
{
    using System;

    [ObsoleteEx(
        Message = "The timeout manager has been removed in favor of native delayed delivery support provided by transports. See the upgrade guide for more details.",
        TreatAsErrorFromVersion = "8",
        RemoveInVersion = "9")]
    public static class ExternalTimeoutManagerConfigurationExtensions
    {
        [ObsoleteEx(
            Message = "The timeout manager has been removed in favor of native delayed delivery support provided by transports. See the upgrade guide for more details.",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        public static void UseExternalTimeoutManager(this EndpointConfiguration endpointConfiguration, string externalTimeoutManagerAddress)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus.Transport
{
    using System.Threading.Tasks;
    using Pipeline;

    [ObsoleteEx(
        Message = "The timeout manager has been removed in favor of native delayed delivery support provided by transports. See the upgrade guide for more details.",
        TreatAsErrorFromVersion = "8",
        RemoveInVersion = "9")]
    public interface ICancelDeferredMessages
    {
        Task CancelDeferredMessages(string messageKey, IBehaviorContext context);
    }
}

namespace NServiceBus.Transport
{
    using System.Threading.Tasks;
    using Extensibility;

    [ObsoleteEx(
        Message = "The IDispatchMessages interface has been removed. See the upgrade guide for more details.",
        ReplacementTypeOrMember = nameof(IMessageDispatcher),
        TreatAsErrorFromVersion = "8",
        RemoveInVersion = "9")]
    public interface IDispatchMessages
    {
        /// <summary>
        /// Dispatches the given operations to the transport.
        /// </summary>
        Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, ContextBag context);
    }
}

namespace NServiceBus.Transport
{
    using System.Threading.Tasks;

    [ObsoleteEx(
        Message = "Queue creation is done by TransportDefinition.Initialize",
        TreatAsErrorFromVersion = "8",
        RemoveInVersion = "9")]
    public interface ICreateQueues
    {
        [ObsoleteEx(
            Message = "Queue creation is done by TransportDefinition.Initialize",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        Task CreateQueueIfNecessary(QueueBindings queueBindings, string identity);
    }
}

namespace NServiceBus.Transport
{
    [ObsoleteEx(
        ReplacementTypeOrMember = nameof(ReceiveSettings),
        TreatAsErrorFromVersion = "8",
        RemoveInVersion = "9")]
    public class PushSettings
    {
    }
}

namespace NServiceBus.Transport
{
    using System;
    using System.Threading.Tasks;

    [ObsoleteEx(
        ReplacementTypeOrMember = nameof(IMessageReceiver),
        TreatAsErrorFromVersion = "8",
        RemoveInVersion = "9")]
    public interface IPushMessages
    {
        [ObsoleteEx(
            ReplacementTypeOrMember = "IMessageReceiver.Initialize",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        Task Init(Func<MessageContext, Task> onMessage,
            Func<ErrorContext, Task<ErrorHandleResult>> onError,
            CriticalError criticalError,
            PushSettings settings);

        [ObsoleteEx(
            ReplacementTypeOrMember = "IMessageReceiver.StartReceive",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        void Start(PushRuntimeSettings limitations);

        [ObsoleteEx(
            ReplacementTypeOrMember = "IMessageReceiver.StopReceive",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        Task Stop();
    }
}

namespace NServiceBus.Transport
{
    using System;

    [ObsoleteEx(
        Message = "This type is no longer necessary when implementing a transport",
        TreatAsErrorFromVersion = "8",
        RemoveInVersion = "9")]
    public class OutboundRoutingPolicy
    {
        [ObsoleteEx(
            Message = "This type is no longer necessary when implementing a transport",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        public OutboundRoutingPolicy(OutboundRoutingType sends, OutboundRoutingType publishes, OutboundRoutingType replies)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "This property is no longer necessary when implementing a transport",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        public OutboundRoutingType Sends => throw new NotImplementedException();

        [ObsoleteEx(
            ReplacementTypeOrMember = "TransportDefinition.SupportsPublishSubscribe",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        public OutboundRoutingType Publishes => throw new NotImplementedException();

        [ObsoleteEx(
            Message = "This property is no longer necessary when implementing a transport",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        public OutboundRoutingType Replies => throw new NotImplementedException();
    }

    [ObsoleteEx(
        Message = "This enum is no longer necessary when implementing a transport",
        TreatAsErrorFromVersion = "8",
        RemoveInVersion = "9")]
    public enum OutboundRoutingType
    {
        [ObsoleteEx(
            Message = "This enum is no longer necessary when implementing a transport",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        Unicast,
        [ObsoleteEx(
            Message = "This enum is no longer necessary when implementing a transport",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        Multicast
    }
}

namespace NServiceBus.Transport
{
    using System;
    using System.Threading.Tasks;

    [ObsoleteEx(
        Message = "This type is no longer necessary when implementing a transport",
        TreatAsErrorFromVersion = "8",
        RemoveInVersion = "9")]
    public class TransportSubscriptionInfrastructure
    {
        [ObsoleteEx(
            Message = "This type is no longer necessary when implementing a transport",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        public TransportSubscriptionInfrastructure(Func<IManageSubscriptions> subscriptionManagerFactory)
        {
            throw new NotImplementedException();
        }
    }

    [ObsoleteEx(
        Message = "This type is no longer necessary when implementing a transport",
        TreatAsErrorFromVersion = "8",
        RemoveInVersion = "9")]
    public class TransportSendInfrastructure
    {
        [ObsoleteEx(
            Message = "This type is no longer necessary when implementing a transport",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        public TransportSendInfrastructure(Func<IMessageDispatcher> dispatcherFactory,
            Func<Task<StartupCheckResult>> preStartupCheck)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            ReplacementTypeOrMember = "TransportInfrastructure.Dispatcher",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        public Func<IMessageDispatcher> DispatcherFactory => throw new NotImplementedException();

        [ObsoleteEx(
            Message = "Pre-startup checks have been removed",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        public Func<Task<StartupCheckResult>> PreStartupCheck => throw new NotImplementedException();
    }
}

namespace NServiceBus.Transport
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;

    [ObsoleteEx(
        ReplacementTypeOrMember = nameof(ISubscriptionManager),
        TreatAsErrorFromVersion = "8",
        RemoveInVersion = "9")]
    public interface IManageSubscriptions
    {
        [ObsoleteEx(
            ReplacementTypeOrMember = "ISubscriptionManager.SubscribeAll",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        Task Subscribe(Type eventType, ContextBag context);

        [ObsoleteEx(
            ReplacementTypeOrMember = "ISubscriptionManager.Unsubscribe",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        Task Unsubscribe(Type eventType, ContextBag context);
    }
}

namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Transport;
    using Routing;

    [ObsoleteEx(
        RemoveInVersion = "9",
        TreatAsErrorFromVersion = "8",
        ReplacementTypeOrMember = nameof(QueueAddress))]
    public struct LogicalAddress
    {
        [ObsoleteEx(
            RemoveInVersion = "9",
            TreatAsErrorFromVersion = "8",
            Message = "Directly construct a QueueAddress.")]
        public static LogicalAddress CreateRemoteAddress(EndpointInstance endpointInstance)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "9",
            TreatAsErrorFromVersion = "8",
            Message = "Directly construct a QueueAddress with the queueName as the BaseAddress.")]
        public static LogicalAddress CreateLocalAddress(string queueName, IReadOnlyDictionary<string, string> properties)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "9",
            TreatAsErrorFromVersion = "8",
            Message = "Directly construct a QueueAddress with the qualifier.")]
        public LogicalAddress CreateQualifiedAddress(string qualifier)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "9",
            TreatAsErrorFromVersion = "8",
            Message = "Directly construct a QueueAddress with the discriminator.")]
        public LogicalAddress CreateIndividualizedAddress(string discriminator)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "9",
            TreatAsErrorFromVersion = "8",
            ReplacementTypeOrMember = "QueueAddress.Qualifier")]
        public string Qualifier => throw new NotImplementedException();

        [ObsoleteEx(
            RemoveInVersion = "9",
            TreatAsErrorFromVersion = "8",
            ReplacementTypeOrMember = nameof(QueueAddress))]
        public EndpointInstance EndpointInstance => throw new NotImplementedException();
    }
}

namespace NServiceBus
{
    using System;
    using Settings;

    public static partial class SettingsExtensions
    {
        [ObsoleteEx(
            RemoveInVersion = "9",
            TreatAsErrorFromVersion = "8",
            ReplacementTypeOrMember = "SettingsExtensions.EndpointQueueName")]
        public static LogicalAddress LogicalAddress(this ReadOnlySettings settings)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus.Transport
{
    using System;
    using System.Collections.Generic;

    public partial class QueueBindings
    {
        [ObsoleteEx(
            Message = "Receiving addresses are automatically registered.",
            RemoveInVersion = "9",
            TreatAsErrorFromVersion = "8")]
        public IReadOnlyCollection<string> ReceivingAddresses => throw new NotImplementedException();

        [ObsoleteEx(
            Message = "Receiving addresses are automatically registered.",
            RemoveInVersion = "9",
            TreatAsErrorFromVersion = "8")]
        public void BindReceiving(string address) => throw new NotImplementedException();
    }
}

namespace NServiceBus.DeliveryConstraints
{
    using System;
    using NServiceBus.Transport;

    [ObsoleteEx(
        ReplacementTypeOrMember = nameof(DispatchProperties),
        RemoveInVersion = "9",
        TreatAsErrorFromVersion = "8")]
    public abstract class DeliveryConstraint
    {
        [ObsoleteEx(
            RemoveInVersion = "9",
            TreatAsErrorFromVersion = "8")]
        protected DeliveryConstraint() { }
    }

    public static class DeliveryConstraintContextExtensions
    {
        [ObsoleteEx(
            ReplacementTypeOrMember = nameof(DispatchProperties),
            RemoveInVersion = "9",
            TreatAsErrorFromVersion = "8")]
        public static void AddDeliveryConstraint(this Extensibility.ContextBag context, DeliveryConstraint constraint) => throw new NotImplementedException();

        [ObsoleteEx(
            ReplacementTypeOrMember = nameof(DispatchProperties),
            RemoveInVersion = "9",
            TreatAsErrorFromVersion = "8")]
        public static System.Collections.Generic.List<DeliveryConstraint> GetDeliveryConstraints(this Extensibility.ContextBag context) => throw new NotImplementedException();

        [ObsoleteEx(
            ReplacementTypeOrMember = nameof(DispatchProperties),
            RemoveInVersion = "9",
            TreatAsErrorFromVersion = "8")]
        public static void RemoveDeliveryConstraint(this Extensibility.ContextBag context, DeliveryConstraint constraint) => throw new NotImplementedException();

        [ObsoleteEx(
            ReplacementTypeOrMember = nameof(DispatchProperties),
            RemoveInVersion = "9",
            TreatAsErrorFromVersion = "8")]
        public static bool TryGetDeliveryConstraint<T>(this Extensibility.ContextBag context, out T constraint)
            where T : DeliveryConstraint => throw new NotImplementedException();

        [ObsoleteEx(
            ReplacementTypeOrMember = nameof(DispatchProperties),
            RemoveInVersion = "9",
            TreatAsErrorFromVersion = "8")]
        public static bool TryRemoveDeliveryConstraint<T>(this Extensibility.ContextBag context, out T constraint)
            where T : DeliveryConstraint => throw new NotImplementedException();
    }
}

namespace NServiceBus.Transport
{
    using System;
    using Settings;

    public static class LogicalAddressExtensions
    {
        [ObsoleteEx(
            ReplacementTypeOrMember = "TransportDefinition.ToTransportAddress",
            RemoveInVersion = "9",
            TreatAsErrorFromVersion = "8")]
        public static string GetTransportAddress(this ReadOnlySettings settings, LogicalAddress logicalAddress) => throw new NotImplementedException();
    }
}

namespace NServiceBus.Pipeline
{
    using System;
    using System.ComponentModel;

    /// <summary>
    /// Provides context for subscription requests.
    /// </summary>
    public partial interface ISubscribeContext
    {
        /// <summary>
        /// The type of the event.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [ObsoleteEx(
            ReplacementTypeOrMember = nameof(EventTypes),
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        Type EventType { get; }
    }
}

namespace NServiceBus.Transport
{
    using System;

    [ObsoleteEx(
        RemoveInVersion = "9",
        TreatAsErrorFromVersion = "8")]
    public class StartupCheckResult
    {
        [ObsoleteEx(
            RemoveInVersion = "9",
            TreatAsErrorFromVersion = "8")]
        public bool Succeeded => throw new NotImplementedException();

        [ObsoleteEx(
            RemoveInVersion = "9",
            TreatAsErrorFromVersion = "8")]
        public string ErrorMessage => throw new NotImplementedException();

        [ObsoleteEx(
            RemoveInVersion = "9",
            TreatAsErrorFromVersion = "8")]
        public static StartupCheckResult Failed(string errorMessage) => throw new NotImplementedException();

        [ObsoleteEx(
            RemoveInVersion = "9",
            TreatAsErrorFromVersion = "8")]
        public static readonly StartupCheckResult Success = new StartupCheckResult();
    }
}

namespace NServiceBus
{
    using System;
    using Transport;

    /// <summary>
    /// Configuration extensions for routing.
    /// </summary>
    public static class RoutingSettingsExtensions
    {
        [ObsoleteEx(
            ReplacementTypeOrMember = "EndpointConfiguration.UseTransport",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        public static RoutingSettings Routing(this TransportExtensions config) => throw new NotImplementedException();

        [ObsoleteEx(
            ReplacementTypeOrMember = "EndpointConfiguration.UseTransport",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        public static RoutingSettings<T> Routing<T>(this TransportExtensions<T> config)
            where T : TransportDefinition => throw new NotImplementedException();
    }
}

namespace NServiceBus
{
    using System;
    using Pipeline;
    using Routing;
    using Transport;

    /// <summary>
    /// Provides extensions for configuring message driven subscriptions.
    /// </summary>
    public static partial class MessageDrivenSubscriptionsConfigExtensions
    {
        [ObsoleteEx(
            ReplacementTypeOrMember = "RoutingExtensions<T>.SubscriptionAuthorizer",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        public static void SubscriptionAuthorizer<T>(this TransportExtensions<T> transportExtensions,
            Func<IIncomingPhysicalMessageContext, bool> authorizer)
            where T : TransportDefinition, IMessageDrivenSubscriptionTransport
            => throw new NotImplementedException();

        [ObsoleteEx(
            ReplacementTypeOrMember = "RoutingExtensions<T>.DisablePublishing",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        public static void DisablePublishing<T>(this TransportExtensions<T> transportExtensions)
            where T : TransportDefinition, IMessageDrivenSubscriptionTransport
            => throw new NotImplementedException();

    }
}

namespace NServiceBus.Pipeline
{
    public partial interface ITransportReceiveContext
    {
        /// <summary>
        /// Allows the pipeline to flag that it has been aborted and the receive operation should be rolled back.
        /// </summary>
        [ObsoleteEx(
            Message = "The AbortReceiveOperation method is no longer supported. See the upgrade guide for more details.",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        void AbortReceiveOperation();
    }
}

namespace NServiceBus
{
    using System;

    partial class TransportReceiveContext
    {
        [ObsoleteEx(
            Message = "The AbortReceiveOperation method is no longer supported. See the upgrade guide for more details.",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        public void AbortReceiveOperation() => throw new NotImplementedException();
    }
}

namespace NServiceBus.Transport
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using NServiceBus.Extensibility;

    public partial class MessageContext
    {
        [ObsoleteEx(
            ReplacementTypeOrMember = "MessageContext(string, Dictionary<string, string>, byte[], TransportTransaction, ContextBag)",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        public MessageContext(string messageId, Dictionary<string, string> headers, byte[] body, TransportTransaction transportTransaction, CancellationTokenSource receiveCancellationTokenSource, ContextBag context) => throw new NotImplementedException();

        [ObsoleteEx(TreatAsErrorFromVersion = "8", RemoveInVersion = "9")]
        public CancellationTokenSource ReceiveCancellationTokenSource => throw new NotImplementedException();

        [ObsoleteEx(ReplacementTypeOrMember = nameof(NativeMessageId), TreatAsErrorFromVersion = "8", RemoveInVersion = "9")]
        public string MessageId { get; }
    }
}

namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;

    public partial class CriticalError
    {
        [ObsoleteEx(
           Message = "Use the overload that accepts a delegate with a cancellation token.",
           TreatAsErrorFromVersion = "8",
           RemoveInVersion = "9")]
        public CriticalError(Func<ICriticalErrorContext, Task> onCriticalErrorAction) => throw new NotImplementedException();
    }
}

namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;

    public partial class CriticalErrorContext : ICriticalErrorContext
    {
        [ObsoleteEx(
            Message = "Use the overload that accepts a delegate with a cancellation token.",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        public CriticalErrorContext(Func<Task> stop, string error, Exception exception) => throw new NotImplementedException();
    }
}

namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Configuration.AdvancedExtensibility;
    using Faults;

    public partial class DelayedRetriesSettings : ExposeSettings
    {
        [ObsoleteEx(
            Message = "Use the overload that accepts a delegate with a cancellation token.",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        public DelayedRetriesSettings OnMessageBeingRetried(Func<DelayedRetryMessage, Task> notificationCallback) => throw new NotImplementedException();
    }
}

namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;

    public static partial class DiagnosticSettingsExtensions
    {
        [ObsoleteEx(
            Message = "Use the overload that accepts a delegate with a cancellation token.",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        public static void CustomDiagnosticsWriter(this EndpointConfiguration config, Func<string, Task> customDiagnosticsWriter) => throw new NotImplementedException();
    }
}

namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Configuration.AdvancedExtensibility;
    using Faults;

    public partial class ImmediateRetriesSettings : ExposeSettings
    {
        [ObsoleteEx(
            Message = "Use the overload that accepts a delegate with a cancellation token.",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        public ImmediateRetriesSettings OnMessageBeingRetried(Func<ImmediateRetryMessage, Task> notificationCallback) => throw new NotImplementedException();
    }
}

namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    public static partial class ReceivePipelineConfigExtensions
    {
        [ObsoleteEx(
            Message = "Use the overload that accepts a delegate with a cancellation token.",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        public static void OnReceivePipelineCompleted(this PipelineSettings pipelineSettings, Func<ReceivePipelineCompleted, Task> subscription) => throw new NotImplementedException();
    }
}

namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Configuration.AdvancedExtensibility;
    using Faults;

    public partial class RetryFailedSettings : ExposeSettings
    {
        [ObsoleteEx(
            Message = "Use the overload that accepts a delegate with a cancellation token.",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        public RetryFailedSettings OnMessageSentToErrorQueue(Func<FailedMessage, Task> notificationCallback) => throw new NotImplementedException();
    }
}

namespace NServiceBus.Transport
{
    using System;
    using Settings;

    public partial class HostSettings
    {
        [ObsoleteEx(
           Message = "Use the overload that accepts a delegate with a cancellation token.",
           TreatAsErrorFromVersion = "8",
           RemoveInVersion = "9")]
        public HostSettings(string name, string hostDisplayName, StartupDiagnosticEntries startupDiagnostic, Action<string, Exception> criticalErrorAction, bool setupInfrastructure, ReadOnlySettings coreSettings = null) => throw new NotImplementedException();
    }
}

namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;

    public static partial class ConfigureCriticalErrorAction
    {
        [ObsoleteEx(
             Message = "Use the overload that accepts a delegate with a cancellation token.",
             TreatAsErrorFromVersion = "8",
             RemoveInVersion = "9")]
        public static void DefineCriticalErrorAction(this EndpointConfiguration endpointConfiguration, Func<ICriticalErrorContext, Task> onCriticalError) => throw new NotImplementedException();
    }
}

namespace NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions
{
    [ObsoleteEx(
        TreatAsErrorFromVersion = "8.0.0",
        RemoveInVersion = "9.0.0")]
    public interface IInitializableSubscriptionStorage : ISubscriptionStorage
    {
        void Init();
    }
}

namespace NServiceBus
{
    [ObsoleteEx(
        TreatAsErrorFromVersion = "8.0.0",
        RemoveInVersion = "9.0.0", ReplacementTypeOrMember = nameof(MessageIntent))]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
    public enum MessageIntentEnum { }
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
}

namespace NServiceBus.Sagas
{
    [ObsoleteEx(
        TreatAsErrorFromVersion = "8.0.0",
        RemoveInVersion = "9.0.0", ReplacementTypeOrMember = "ISagaFinder<TSagaData, TMessage>")]
#pragma warning disable PS0024 // A non-interface type should not be prefixed with I
    public abstract class IFindSagas<T> { }
#pragma warning restore PS0024 // A non-interface type should not be prefixed with I
}

namespace NServiceBus.Extensibility
{
    [ObsoleteEx(
        TreatAsErrorFromVersion = "8.0.0",
        RemoveInVersion = "9.0.0", ReplacementTypeOrMember = nameof(IReadOnlyContextBag))]
#pragma warning disable IDE1006 // Naming Styles
    public interface ReadOnlyContextBag { }
#pragma warning restore IDE1006 // Naming Styles
}

namespace NServiceBus.Persistence
{
    [ObsoleteEx(
        TreatAsErrorFromVersion = "8.0.0",
        RemoveInVersion = "9.0.0", ReplacementTypeOrMember = nameof(ISynchronizedStorageSession))]
#pragma warning disable IDE1006 // Naming Styles
    public interface SynchronizedStorageSession { }
#pragma warning restore IDE1006 // Naming Styles
}

namespace NServiceBus.Persistence
{
    [ObsoleteEx(
        TreatAsErrorFromVersion = "8.0.0",
        RemoveInVersion = "9.0.0", ReplacementTypeOrMember = nameof(ICompletableSynchronizedStorageSession))]
#pragma warning disable IDE1006 // Naming Styles
    public interface CompletableSynchronizedStorageSession { }
#pragma warning restore IDE1006 // Naming Styles
}

namespace NServiceBus.Outbox
{
    [ObsoleteEx(
        TreatAsErrorFromVersion = "8.0.0",
        RemoveInVersion = "9.0.0", ReplacementTypeOrMember = nameof(IOutboxTransaction))]
#pragma warning disable IDE1006 // Naming Styles
    public interface OutboxTransaction { }
#pragma warning restore IDE1006 // Naming Styles
}

namespace NServiceBus.Settings
{
    [ObsoleteEx(
        TreatAsErrorFromVersion = "8.0.0",
        RemoveInVersion = "9.0.0", ReplacementTypeOrMember = nameof(IReadOnlySettings))]
#pragma warning disable IDE1006 // Naming Styles
    public interface ReadOnlySettings { }
#pragma warning restore IDE1006 // Naming Styles
}
#pragma warning restore 1591
