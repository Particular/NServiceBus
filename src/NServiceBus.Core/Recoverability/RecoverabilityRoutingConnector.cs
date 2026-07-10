#nullable enable

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
        var recoverabilityActionContext = context.PreventChanges();
        var recoverabilityAction = context.RecoverabilityAction;
        var routingContexts = recoverabilityAction
            .GetRoutingContexts(recoverabilityActionContext);

        foreach (var routingContext in routingContexts)
        {
            await stage(routingContext).ConfigureAwait(false);
        }

        var activity = Activity.Current;

        if (context.RecoverabilityAction is ImmediateRetry)
        {
            incomingPipelineMetrics.RecordImmediateRetry(context);
            activity?.AddTag("NServiceBus.RecoverabilityAction", "immediate_retry");
            activity?.DisplayName += " immediate retry";

        }
        else if (context.RecoverabilityAction is DelayedRetry)
        {
            incomingPipelineMetrics.RecordDelayedRetry(context);
            activity?.AddTag("NServiceBus.RecoverabilityAction", "delayed_retry");
            activity?.DisplayName += " delayed retry";
        }
        else if (context.RecoverabilityAction is MoveToError)
        {
            incomingPipelineMetrics.RecordSendToErrorQueue(context);
            activity?.AddTag("NServiceBus.RecoverabilityAction", "move_to_error");
            activity?.DisplayName += " move to error queue";
        }
        else if (context.RecoverabilityAction is Discard)
        {
            activity?.AddTag("NServiceBus.RecoverabilityAction", "discard");
            activity?.DisplayName += " discard";
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