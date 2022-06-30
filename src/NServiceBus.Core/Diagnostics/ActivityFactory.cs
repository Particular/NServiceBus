namespace NServiceBus;

using System.Diagnostics;
using Pipeline;
using Sagas;
using Transport;

class ActivityFactory : IActivityFactory
{
    public Activity StartIncomingActivity(MessageContext context)
    {
        Activity activity;
        if (context.Extensions.TryGet(out Activity transportActivity)) // attach to transport span but link receive pipeline to send pipeline spa
        {
            ActivityLink[] links = null;
            if (context.Headers.TryGetValue(Headers.DiagnosticsTraceParent, out var sendSpanId) && sendSpanId != transportActivity.Id)
            {
                if (ActivityContext.TryParse(sendSpanId, null, out var sendSpanContext))
                {
                    links = new[] { new ActivityLink(sendSpanContext) };
                }
            }

            activity = ActivitySources.Main.CreateActivity(name: ActivityNames.IncomingMessageActivityName,
                ActivityKind.Consumer, transportActivity.Context, links: links, idFormat: ActivityIdFormat.W3C);

        }
        else if (context.Headers.TryGetValue(Headers.DiagnosticsTraceParent, out var sendSpanId) && ActivityContext.TryParse(sendSpanId, null, out var sendSpanContext)) // otherwise directly create child from logical send
        {
            activity = ActivitySources.Main.CreateActivity(name: ActivityNames.IncomingMessageActivityName, ActivityKind.Consumer, sendSpanContext);
        }
        else // otherwise start new trace
        {
            // This will use Activity.Current if set
            activity = ActivitySources.Main.CreateActivity(name: ActivityNames.IncomingMessageActivityName, ActivityKind.Consumer);

        }

        ContextPropagation.PropagateContextFromHeaders(activity, context.Headers);

        if (activity != null)
        {
            activity.DisplayName = "process message";
            activity.SetIdFormat(ActivityIdFormat.W3C);
            activity.AddTag("nservicebus.native_message_id", context.NativeMessageId);

            ActivityDecorator.PromoteHeadersToTags(activity, context.Headers);

            activity.Start();
        }

        return activity;
    }

    public Activity StartOutgoingPipelineActivity(string activityName, string displayName, IBehaviorContext outgoingContext)
    {

        var activity = ActivitySources.Main.CreateActivity(activityName, ActivityKind.Producer);

        if (activity != null)
        {
            activity.SetIdFormat(ActivityIdFormat.W3C);
            activity.DisplayName = displayName;
            activity.Start();

            outgoingContext.Extensions.SetPipelineActitvity(activity);
        }

        return activity;
    }

    public Activity StartHandlerActivity(MessageHandler messageHandler, ActiveSagaInstance saga)
    {
        var activity = ActivitySources.Main.StartActivity(ActivityNames.InvokeHandlerActivityName);

        if (activity != null)
        {
            activity.DisplayName = messageHandler.HandlerType.Name;
            ActivityDecorator.SetInvokeHandlerTags(activity, messageHandler, saga); //TODO inline decorator logic?
        }

        return activity;
    }
}