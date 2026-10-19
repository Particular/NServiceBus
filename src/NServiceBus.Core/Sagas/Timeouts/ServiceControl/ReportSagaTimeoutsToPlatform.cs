#nullable enable

namespace NServiceBus;

using Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NServiceBus.Features;
using Outbox;
using NServiceBus.ServicePlatform;
using Sagas;
using Transport;
using Unicast.Messages;

class ReportSagaTimeoutsToPlatform : Feature
{
    public ReportSagaTimeoutsToPlatform()
    {
        DependsOn<Features.ServicePlatform>();
        DependsOn<Features.Sagas>();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        var endpointName = context.Settings.EndpointName();
        var managementQueue = new QueueAddress(context.Receiving.LocalQueueAddress.BaseAddress, qualifier: ManagementQueueQualifier);

        context.Services.AddSingleton(serviceProvider => new SagaTimeoutPlatformNotifier(
            serviceProvider.GetRequiredService<ServicePlatformConnection>(),
            endpointName,
            serviceProvider.GetRequiredService<HostInformation>(),
            serviceProvider.GetRequiredService<ITransportAddressResolver>().ToTransportAddress(managementQueue)));

        context.Services.AddSingleton(serviceProvider => new SagaTimeoutManagementSatellite(
            serviceProvider.GetRequiredService<ISagaPersister>(),
            context.Settings.Get<SagaMetadataCollection>(),
            serviceProvider.GetRequiredService<MessageMetadataRegistry>(),
            serviceProvider.GetRequiredService<TimeoutSerializer>(),
            serviceProvider.GetRequiredService<IMessageDispatcher>(),
            // Without the outbox enabled the storage is a no-op, as in the receive pipeline
            serviceProvider.GetService<IOutboxStorage>() ?? new NoOpOutboxStorage(),
            serviceProvider.GetRequiredService<SagaTimeoutPlatformNotifier>(),
            endpointName,
            serviceProvider.GetRequiredService<HostInformation>(),
            serviceProvider.GetRequiredService<ReceiveAddresses>().MainReceiveAddress,
            serviceProvider.GetRequiredService<ILogger<SagaTimeoutManagementSatellite>>()));

        context.AddSatelliteReceiver(
            "Saga timeout management",
            managementQueue,
            PushRuntimeSettings.Default,
            static (config, errorContext) => errorContext.Exception is InvalidSagaTimeoutManagementRequestException
                ? RecoverabilityAction.MoveToError(config.Failed.ErrorQueue)
                : DefaultRecoverabilityPolicy.Invoke(config, errorContext),
            static (serviceProvider, messageContext, cancellationToken) =>
                serviceProvider.GetRequiredService<SagaTimeoutManagementSatellite>().Handle(serviceProvider, messageContext, cancellationToken));
    }

    public const string ManagementQueueQualifier = "SagaTimeouts";
}
