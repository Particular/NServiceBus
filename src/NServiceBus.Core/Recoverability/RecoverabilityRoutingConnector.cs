namespace NServiceBus;

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Pipeline;

class RecoverabilityRoutingConnector : StageConnector<IRecoverabilityContext, IRoutingContext>
{
    readonly IncomingPipelineMetrics incomingPipelineMetrics;

    public RecoverabilityRoutingConnector(
        IncomingPipelineMetrics incomingPipelineMetrics,
        INotificationSubscriptions<MessageToBeRetried> messageRetryNotification,
        INotificationSubscriptions<MessageFaulted> messageFaultedNotification)
    {
        this.incomingPipelineMetrics = incomingPipelineMetrics;
        notifications = new CompositeNotification();
        notifications.Register(messageRetryNotification);
        notifications.Register(messageFaultedNotification);
    }

    public override async Task Invoke(IRecoverabilityContext context, Func<IRoutingContext, Task> stage)
    {
        var availableMetricTags = context.Extensions.Get<IncomingPipelineMetricTags>();
        var recoverabilityActionContext = context.PreventChanges();
        var recoverabilityAction = context.RecoverabilityAction;
        var routingContexts = recoverabilityAction
            .GetRoutingContexts(recoverabilityActionContext);

        foreach (var routingContext in routingContexts)
        {
            await stage(routingContext).ConfigureAwait(false);
        }

        var tags = new TagList();

        availableMetricTags.ApplyTags(ref tags,
            [MeterTags.EndpointDiscriminator, MeterTags.QueueName, MeterTags.FailureType, MeterTags.MessageType, MeterTags.MessageHandlerTypes]);

        if (context.RecoverabilityAction is ImmediateRetry)
        {
            incomingPipelineMetrics.RecordImmediateRetry(context);
        }
        else if (context.RecoverabilityAction is DelayedRetry)
        {
            incomingPipelineMetrics.RecordDelayedRetry(context);
        }
        else if (context.RecoverabilityAction is MoveToError)
        {
            incomingPipelineMetrics.RecordSendToErrorQueue(context);
        }

        if (context is IRecoverabilityActionContextNotifications events)
        {
            foreach (object @event in events)
            {
                await notifications.Raise(@event, context.CancellationToken).ConfigureAwait(false);
            }
        }
    }

    readonly CompositeNotification notifications;
}
