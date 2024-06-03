namespace NServiceBus;

using System.Diagnostics;
using Pipeline;
using Sagas;
using Transport;

class ActivityFactory : IActivityFactory
{
    public Activity StartIncomingPipelineActivity(MessageContext context)
    {
        Activity activity;
        var incomingTraceParentExists = context.Headers.TryGetValue(Headers.DiagnosticsTraceParent, out var sendSpanId);
        var activityContextCreatedFromIncomingTraceParent = ActivityContext.TryParse(sendSpanId, null, out var sendSpanContext);

        if (context.Extensions.TryGet(out Activity transportActivity) && transportActivity != null)
        {
            // attach to transport span but link receive pipeline span to send pipeline span
            ActivityLink[] links = null;
            if (incomingTraceParentExists && sendSpanId != transportActivity.Id)
            {
                if (activityContextCreatedFromIncomingTraceParent)
                {
                    links = [new ActivityLink(sendSpanContext)];
                }
            }

            if (context.Headers.ContainsKey(Headers.StartNewTrace))
            {
                // The user indicated to start a new trace when receiving this message but there's a transport span
                // so check whether a new trace was already started by the transport client SDK
                if (incomingTraceParentExists && !sendSpanContext.TraceId.Equals(transportActivity.TraceId))
                {
                    // no new trace was started, so start a new one
                    activity = ActivitySources.Main.StartActivity(name: ActivityNames.IncomingMessageActivityName, ActivityKind.Consumer, CreateNewRootActivityContext(), tags: null, links: links);
                }
            }
            else
            {
                // no new trace was requested, so create a child span of the transport span, linking receive pipeline span to send pipeline span
                activity = ActivitySources.Main.CreateActivity(name: ActivityNames.IncomingMessageActivityName,
                    ActivityKind.Consumer, transportActivity.Context, links: links, idFormat: ActivityIdFormat.W3C);
            }
        }
        else if (incomingTraceParentExists && activityContextCreatedFromIncomingTraceParent) // otherwise directly create child from logical send
        {
            if (IsMessageDelayed(context))
            {
                // this is a delayed message and should therefore start a new trace and only link to the originating span
                ActivityLink[] links = [new ActivityLink(sendSpanContext)];
                // create a new trace or root activity
                activity = ActivitySources.Main.StartActivity(name: ActivityNames.IncomingMessageActivityName, ActivityKind.Consumer, CreateNewRootActivityContext(), tags: null, links: links);
            }
            else
            {
                // this is a regular message and should therefore start a child trace
                ActivityContext.TryParse(sendSpanId, null, true, out var remoteParentActivityContext);
                activity = ActivitySources.Main.CreateActivity(name: ActivityNames.IncomingMessageActivityName, ActivityKind.Consumer, remoteParentActivityContext);
            }
        }
        else // otherwise start new trace
        {
            // This will set Activity.Current as parent if available
            activity = ActivitySources.Main.CreateActivity(name: ActivityNames.IncomingMessageActivityName, ActivityKind.Consumer);
        }

        if (activity != null)
        {
            ContextPropagation.PropagateContextFromHeaders(activity, context.Headers);

            activity.DisplayName = ActivityDisplayNames.ProcessMessage;
            activity.SetIdFormat(ActivityIdFormat.W3C);
            activity.AddTag(ActivityTags.NativeMessageId, context.NativeMessageId);

            ActivityDecorator.PromoteHeadersToTags(activity, context.Headers);

            activity.Start();
        }

        return activity;
    }

    /// <summary>
    /// Message can be delayed due to requesting a saga timeout, deferring a message through send options or delayed retries.
    /// Saga timeout and message deferral will result in the DeliverAt header set
    /// Delayed retry will result in the DelayedRetries header set
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    static bool IsMessageDelayed(MessageContext context) =>
        context.Headers.ContainsKey(Headers.DeliverAt) ||
        context.Headers.ContainsKey(Headers.DelayedRetries);

    static ActivityContext CreateNewRootActivityContext() => new(Activity.TraceIdGenerator is null ? ActivityTraceId.CreateRandom() : Activity.TraceIdGenerator(), default, default, default);

    public Activity StartOutgoingPipelineActivity(string activityName, string displayName, IBehaviorContext outgoingContext)
    {
        var activity = ActivitySources.Main.CreateActivity(activityName, ActivityKind.Producer);

        if (activity != null)
        {
            activity.SetIdFormat(ActivityIdFormat.W3C);
            activity.DisplayName = displayName;
            activity.Start();

            outgoingContext.Extensions.SetOutgoingPipelineActitvity(activity);
        }

        return activity;
    }

    public Activity StartHandlerActivity(MessageHandler messageHandler, ActiveSagaInstance saga)
    {
        if (Activity.Current == null)
        {
            // don't call StartActivity if we haven't started an activity from the incoming pipeline to avoid the handlers being sampled although the incoming message isn't.
            return null;
        }

        var activity = ActivitySources.Main.StartActivity(ActivityNames.InvokeHandlerActivityName);

        if (activity != null)
        {
            activity.DisplayName = messageHandler.HandlerType.Name;
            activity.AddTag(ActivityTags.HandlerType, messageHandler.HandlerType.FullName);

            if (saga != null)
            {
                activity.AddTag(ActivityTags.HandlerSagaId, saga.SagaId);
            }
        }

        return activity;
    }
}