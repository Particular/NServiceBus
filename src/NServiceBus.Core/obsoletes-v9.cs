#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace NServiceBus
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus.Extensibility;
    using NServiceBus.Pipeline;
    using NServiceBus.Settings;
    using NServiceBus.Transport;

    partial class AuditContext
    {
        [ObsoleteEx(
          ReplacementTypeOrMember = nameof(AuditMetadata),
          TreatAsErrorFromVersion = "9.0",
          RemoveInVersion = "10.0")]
        public void AddAuditData(string key, string value) => throw new NotImplementedException();
    }

    public static partial class ConnectorContextExtensions
    {
        [ObsoleteEx(
            TreatAsErrorFromVersion = "9.0",
            RemoveInVersion = "10.0",
            ReplacementTypeOrMember = "CreateAuditContext(this ForkConnector<IIncomingPhysicalMessageContext, IAuditContext> forkConnector, OutgoingMessage message, string auditAddress, TimeSpan? timeToBeReceived, IIncomingPhysicalMessageContext sourceContext)")]
        public static IAuditContext CreateAuditContext(this ForkConnector<IIncomingPhysicalMessageContext, IAuditContext> forkConnector, OutgoingMessage message, string auditAddress, IIncomingPhysicalMessageContext sourceContext) => throw new NotImplementedException();
    }

    public partial class EndpointConfiguration
    {

        [ObsoleteEx(
            Message = "Error notification events have been replaced with a Task-based API available on the recoverability settings.",
            TreatAsErrorFromVersion = "9",
            RemoveInVersion = "10")]
        public Notifications Notifications { get; }
    }

    public static partial class ImmediateDispatchOptionExtensions
    {
        [ObsoleteEx(
            ReplacementTypeOrMember = nameof(IsImmediateDispatchSet),
            RemoveInVersion = "10.0",
            TreatAsErrorFromVersion = "9.0")]
        public static bool RequiredImmediateDispatch(this ExtendableOptions options) => throw new NotImplementedException();
    }
    public partial class LearningTransport
    {
        [ObsoleteEx(
            Message = "Inject the ITransportAddressResolver type to access the address translation mechanism at runtime. See the NServiceBus version 8 upgrade guide for further details.",
            TreatAsErrorFromVersion = "9",
            RemoveInVersion = "10")]
        public override string ToTransportAddress(QueueAddress queueAddress) => throw new NotImplementedException();
    }

    [ObsoleteEx(
      Message = "Error notification events have been replaced with a Task-based API available on the recoverability settings.",
      TreatAsErrorFromVersion = "9",
      RemoveInVersion = "10")]
    public class Notifications { }

    [ObsoleteEx(
        Message = "Use methods on IServiceCollection instead. Note that interfaces are not registered implicitly. See the NServiceBus 7 to 8 upgrade guide for more information.",
        TreatAsErrorFromVersion = "9.0",
        RemoveInVersion = "10.0")]
    public static class ServiceCollectionExtensions
    {
        [ObsoleteEx(
            ReplacementTypeOrMember = "IServiceCollection.Add",
            TreatAsErrorFromVersion = "9.0",
            RemoveInVersion = "10.0")]
        public static void ConfigureComponent(this IServiceCollection serviceCollection, Type concreteComponent, DependencyLifecycle dependencyLifecycle) => throw new NotImplementedException();

        [ObsoleteEx(
            Message = "Use `IServiceCollection.Add`, `IServiceCollection.AddSingleton`, `IServiceCollection.AddTransient` or `IServiceCollection.AddScoped` instead. Note that interfaces are not registered implicitly. See the NServiceBus 7 to 8 upgrade guide for more information.",
            TreatAsErrorFromVersion = "9.0",
            RemoveInVersion = "10.0")]
        public static void ConfigureComponent<T>(this IServiceCollection serviceCollection, DependencyLifecycle dependencyLifecycle) => throw new NotImplementedException();

        [ObsoleteEx(
            Message = "Use `IServiceCollection.Add`, `IServiceCollection.AddSingleton`, `IServiceCollection.AddTransient` or `IServiceCollection.AddScoped` instead. Note that interfaces are not registered implicitly. See the NServiceBus 7 to 8 upgrade guide for more information.",
            TreatAsErrorFromVersion = "9.0",
            RemoveInVersion = "10.0")]
        public static void ConfigureComponent<T>(this IServiceCollection serviceCollection, Func<T> componentFactory, DependencyLifecycle dependencyLifecycle) => throw new NotImplementedException();

        [ObsoleteEx(
            Message = "Use `IServiceCollection.Add`, `IServiceCollection.AddSingleton`, `IServiceCollection.AddTransient` or `IServiceCollection.AddScoped` instead. Note that interfaces are not registered implicitly. See the NServiceBus 7 to 8 upgrade guide for more information.",
            TreatAsErrorFromVersion = "9.0",
            RemoveInVersion = "10.0")]
        public static void ConfigureComponent<T>(this IServiceCollection serviceCollection, Func<IServiceProvider, T> componentFactory, DependencyLifecycle dependencyLifecycle) => throw new NotImplementedException();

        [ObsoleteEx(
            Message = "Use `IServiceCollection.Add`, `IServiceCollection.AddSingleton`, `IServiceCollection.AddTransient` or `IServiceCollection.AddScoped` instead.",
            TreatAsErrorFromVersion = "9.0",
            RemoveInVersion = "10.0")]
        public static void RegisterSingleton(this IServiceCollection serviceCollection, Type lookupType, object instance) => throw new NotImplementedException();

        [ObsoleteEx(
            ReplacementTypeOrMember = "IServiceCollection.AddSingleton",
            TreatAsErrorFromVersion = "9.0",
            RemoveInVersion = "10.0")]
        public static void RegisterSingleton<T>(this IServiceCollection serviceCollection, T instance) => throw new NotImplementedException();

        [ObsoleteEx(
            ReplacementTypeOrMember = "IServiceCollection.GetEnumerator",
            TreatAsErrorFromVersion = "9.0",
            RemoveInVersion = "10.0")]
        public static bool HasComponent<T>(this IServiceCollection serviceCollection) => throw new NotImplementedException();

        [ObsoleteEx(
            ReplacementTypeOrMember = "IServiceCollection.GetEnumerator",
            TreatAsErrorFromVersion = "9.0",
            RemoveInVersion = "10.0")]
        public static bool HasComponent(this IServiceCollection serviceCollection, Type componentType) => throw new NotImplementedException();
    }

    public static partial class SettingsExtensions
    {
        [ObsoleteEx(
            Message = "Use FeatureConfigurationContext.LocalQueueAddress() to access the endpoint queue address. Inject the ReceiveAddresses class to access the endpoint's receiving transport addresses at runtime. See the NServiceBus version 8 upgrade guide for further details.",
            TreatAsErrorFromVersion = "9",
            RemoveInVersion = "10")]
        public static string LocalAddress(this IReadOnlySettings settings) => throw new NotImplementedException();

        [ObsoleteEx(
            Message = "Use FeatureConfigurationContext.InstanceSpecificQueueAddress() to access the endpoint instance specific queue address. Inject the ReceiveAddresses class to access the endpoint's receiving transport addresses at runtime. See the NServiceBus version 8 upgrade guide for further details.",
            TreatAsErrorFromVersion = "9",
            RemoveInVersion = "10")]
        public static string InstanceSpecificQueue(this IReadOnlySettings settings) => throw new NotImplementedException();
    }
}

namespace NServiceBus.Features
{
    using System;
    using Microsoft.Extensions.DependencyInjection;

    public partial class FeatureConfigurationContext
    {
        [ObsoleteEx(
            ReplacementTypeOrMember = nameof(Services),
            TreatAsErrorFromVersion = "9.0",
            RemoveInVersion = "10.0")]
        public IServiceCollection Container => throw new NotImplementedException();
    }
}

namespace NServiceBus.ObjectBuilder
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.DependencyInjection;

    [ObsoleteEx(
       TreatAsErrorFromVersion = "9",
       RemoveInVersion = "10")]
    public static class ServiceProviderExtensions
    {
        [ObsoleteEx(
            ReplacementTypeOrMember = "IServiceProvider.GetService",
            TreatAsErrorFromVersion = "9",
            RemoveInVersion = "10")]
        public static object Build(this IServiceProvider serviceProvider, Type typeToBuild) => throw new NotImplementedException();

        [ObsoleteEx(
            ReplacementTypeOrMember = "IServiceProvider.CreateScope",
            TreatAsErrorFromVersion = "9",
            RemoveInVersion = "10")]
        public static IServiceScope CreateChildBuilder(this IServiceProvider serviceProvider) => throw new NotImplementedException();

        [ObsoleteEx(
            ReplacementTypeOrMember = "IServiceProvider.GetService",
            TreatAsErrorFromVersion = "9",
            RemoveInVersion = "10")]
        public static T Build<T>(this IServiceProvider serviceProvider) => throw new NotImplementedException();

        [ObsoleteEx(
            ReplacementTypeOrMember = "IServiceProvider.GetServices",
            TreatAsErrorFromVersion = "9",
            RemoveInVersion = "10")]
        public static IEnumerable<T> BuildAll<T>(this IServiceProvider serviceProvider) => throw new NotImplementedException();

        [ObsoleteEx(
            ReplacementTypeOrMember = "IServiceProvider.GetServices",
            TreatAsErrorFromVersion = "9",
            RemoveInVersion = "10")]
        public static IEnumerable<object> BuildAll(this IServiceProvider serviceProvider, Type typeToBuild) => throw new NotImplementedException();
    }
}

namespace NServiceBus.Pipeline
{
    public partial interface IAuditContext : IBehaviorContext
    {
        [ObsoleteEx(
            ReplacementTypeOrMember = nameof(AuditMetadata),
            TreatAsErrorFromVersion = "9.0",
            RemoveInVersion = "10.0")]
        void AddAuditData(string key, string value);
    }
}

namespace NServiceBus.Support
{
    using System;

    public static partial class RuntimeEnvironment
    {
        [ObsoleteEx(
            ReplacementTypeOrMember = "HostInfoSettings.UsingHostName",
            TreatAsErrorFromVersion = "9.0",
            RemoveInVersion = "10.0")]
        public static Func<string> MachineNameAction { get; set; }
    }
}

namespace NServiceBus.Transport
{
    public abstract partial class TransportDefinition
    {
        [ObsoleteEx(
            Message = "Inject the ITransportAddressResolver type to access the address translation mechanism at runtime. See the NServiceBus version 8 upgrade guide for further details.",
            TreatAsErrorFromVersion = "9",
            RemoveInVersion = "10")]
        public abstract string ToTransportAddress(QueueAddress address);
    }
}

namespace NServiceBus.UnitOfWork
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;

    [ObsoleteEx(Message = "The unit of work pattern is more straightforward to implement in a pipeline behavior, where the using keyword and try/catch blocks can be used.", ReplacementTypeOrMember = "NServiceBus.Pipeline.Behavior<TContext>", TreatAsErrorFromVersion = "9", RemoveInVersion = "10")]
    [SuppressMessage("Code", "PS0018:A task-returning method should have a CancellationToken parameter unless it has a parameter implementing ICancellableContext", Justification = "Obsolete.")]
    public interface IManageUnitsOfWork
    {
        Task Begin();

        Task End(Exception ex = null);
    }
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member