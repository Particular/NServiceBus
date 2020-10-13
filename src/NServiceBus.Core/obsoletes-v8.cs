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
    using Container;
    using ObjectBuilder.Common;

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
        private ContainerCustomizations()
        {
            // private ctor
        }
    }
}

namespace NServiceBus.ObjectBuilder
{
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

namespace NServiceBus
{
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
    using DeliveryConstraints;

    [ObsoleteEx(
        Message = "Non-durable delivery support has been moved to the transports that can support it. See the upgrade guide for more details.",
        TreatAsErrorFromVersion = "8",
        RemoveInVersion = "9")]
    public class NonDurableDelivery2 : DeliveryConstraint
    {
    }
}

namespace NServiceBus
{
    using Settings;
    using System;

    [ObsoleteEx(
        Message = "Non-durable delivery support has been moved to the transports that can support it. See the upgrade guide for more details.",
        TreatAsErrorFromVersion = "8",
        RemoveInVersion = "9")]
    public static class DurableMessagesConfig
    {
        public static void EnableDurableMessages(this EndpointConfiguration config)
        {
            throw new NotImplementedException();
        }

        public static void DisableDurableMessages(this EndpointConfiguration config)
        {
            throw new NotImplementedException();
        }

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

#pragma warning restore 1591
