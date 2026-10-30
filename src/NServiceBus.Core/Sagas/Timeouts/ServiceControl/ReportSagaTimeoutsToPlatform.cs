#nullable enable

namespace NServiceBus;

using System;
using System.Threading.Tasks;
using Hosting;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Features;
using Pipeline;
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
        if (!context.SagaStorageSupportsTimeoutManagement)
        {
            throw new Exception($"Saga timeout reporting to ServiceControl requires a saga persistence that supports timeout management, but the selected saga persistence '{context.SagaPersistenceName}' doesn't. Use a persistence that supports timeout management, or remove the call to 'ReportSagaTimeoutsToServiceControl()'.");
        }

        var endpointName = context.Settings.EndpointName();
        var controlQueue = new QueueAddress(context.Receiving.LocalQueueAddress.BaseAddress, qualifier: ControlQueueQualifier);

        context.Services.AddSingleton(serviceProvider => new SagaTimeoutPlatformNotifier(
            serviceProvider.GetRequiredService<ServicePlatformConnection>(),
            endpointName,
            serviceProvider.GetRequiredService<HostInformation>(),
            serviceProvider.GetRequiredService<ITransportAddressResolver>().ToTransportAddress(controlQueue)));

        var sagas = context.Settings.Get<SagaMetadataCollection>();
        context.Services.AddSingleton(serviceProvider => new SagaTimeoutChangeRequestHandler.EndpointInfo(
            endpointName,
            serviceProvider.GetRequiredService<ReceiveAddresses>().MainReceiveAddress,
            sagas));

        // Control messages are handled by Core's own handler, and their type is known even when it isn't a message by convention
        context.Settings.Get<MessageMetadataRegistry>().RegisterMessageTypesBypassingChecks([typeof(SagaTimeoutChangeRequest)]);
        context.Settings.Get<ReceiveComponent.Settings>().MessageHandlerRegistry.AddMessageHandlerForMessage<SagaTimeoutChangeRequestHandler, SagaTimeoutChangeRequest>();

        // The control queue runs the main receive pipeline, unlike a satellite, with a low concurrency limit of its own
        context.Receiving.AddMainPipelineReceiver(ControlReceiverName, controlQueue, new PushRuntimeSettings(ControlQueueMaxConcurrency));
        context.Pipeline.Register(new RejectNonControlMessagesBehavior(), "Rejects messages other than control messages received on the control queue");
    }

    // The control queue runs the main pipeline, so without this, any handler of the endpoint would process a business message sent there by mistake
    sealed class RejectNonControlMessagesBehavior : IBehavior<IIncomingLogicalMessageContext, IIncomingLogicalMessageContext>
    {
        public Task Invoke(IIncomingLogicalMessageContext context, Func<IIncomingLogicalMessageContext, Task> next) =>
            context.Extensions.TryGet<AdditionalPipelineReceiver>(out var receiver) && receiver.Name == ControlReceiverName && context.Message.MessageType != typeof(SagaTimeoutChangeRequest)
                ? throw new Exception($"The control queue only accepts control messages, but received '{context.Message.MessageType.FullName}'.")
                : next(context);
    }

    public const string ControlQueueQualifier = "control";
    const string ControlReceiverName = "Control";
    const int ControlQueueMaxConcurrency = 1;
}
