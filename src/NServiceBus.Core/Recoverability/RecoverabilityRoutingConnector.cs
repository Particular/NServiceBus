#nullable enable

namespace NServiceBus;

using System;
using System.Threading.Tasks;
using Pipeline;

class RecoverabilityRoutingConnector : StageConnector<IRecoverabilityContext, IRoutingContext>
{
    readonly PipelineMetrics pipelineMetrics;

    public RecoverabilityRoutingConnector(
        PipelineMetrics pipelineMetrics,
        INotificationSubscriptions<MessageToBeRetried> messageRetryNotification,
        INotificationSubscriptions<MessageFaulted> messageFaultedNotification)
    {
        this.pipelineMetrics = pipelineMetrics;
        notifications = new CompositeNotification();
        notifications.Register(messageRetryNotification);
        notifications.Register(messageFaultedNotification);
    }

    public override async Task Invoke(IRecoverabilityContext context, Func<IRoutingContext, Task> stage)
    {
        var recoverabilityActionContext = context.PreventChanges();
        var recoverabilityAction = context.RecoverabilityAction;
        var routingContexts = recoverabilityAction
            .GetRoutingContexts(recoverabilityActionContext);

        foreach (var routingContext in routingContexts)
        {
            await stage(routingContext).ConfigureAwait(false);
        }

        if (context.RecoverabilityAction is ImmediateRetry)
        {
            pipelineMetrics.RecordImmediateRetry(context);
        }
        else if (context.RecoverabilityAction is DelayedRetry)
        {
            pipelineMetrics.RecordDelayedRetry(context);
        }
        else if (context.RecoverabilityAction is MoveToError)
        {
            pipelineMetrics.RecordSendToErrorQueue(context);
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