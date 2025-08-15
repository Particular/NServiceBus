#nullable enable

namespace NServiceBus;

using System.Diagnostics;
using Pipeline;
using Transport;

class ActivityFactory : IActivityFactory
{
    public Activity? StartIncomingPipelineActivity(MessageContext context)
    {
        Activity? activity;
        var incomingTraceParentExists = context.Headers.TryGetValue(Headers.DiagnosticsTraceParent, out var sendSpanId);
        var activityContextCreatedFromIncomingTraceParent = ActivityContext.TryParse(sendSpanId, null, out var sendSpanContext);

        if (context.Extensions.TryGet<Activity>(out var transportActivity)) // attach to transport span but link receive pipeline span to send pipeline span
        {
            ActivityLink[]? links = null;
            if (incomingTraceParentExists && sendSpanId != transportActivity.Id)
            {
                if (activityContextCreatedFromIncomingTraceParent)
                {
                    links = [new ActivityLink(sendSpanContext)];
                }
            }

            activity = ActivitySources.Main.CreateActivity(name: ActivityNames.IncomingMessageActivityName,
                ActivityKind.Consumer, transportActivity.Context, links: links, idFormat: ActivityIdFormat.W3C);

        }
        else if (incomingTraceParentExists && activityContextCreatedFromIncomingTraceParent) // otherwise directly create child from logical send
        {
            var isStartNewTraceHeaderAvailable = context.Headers.TryGetValue(Headers.StartNewTrace, out var shouldStartNewTrace);
            if (isStartNewTraceHeaderAvailable && shouldStartNewTrace?.Equals(bool.TrueString) is true)
            {
                // create a new trace or root activity
                ActivityLink[] links = [new ActivityLink(sendSpanContext)];
                //null the current activity so that the new one is created as root https://github.com/dotnet/runtime/issues/65528#issuecomment-2613486896
                Activity.Current = null;
                activity = ActivitySources.Main.StartActivity(name: ActivityNames.IncomingMessageActivityName, ActivityKind.Consumer, parentContext: default, tags: null, links: links);
            }
            else
            {
                // no new trace was requested, so start a child trace
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

    public Activity? StartOutgoingPipelineActivity(string activityName, string displayName, IBehaviorContext outgoingContext)
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

    public Activity? StartHandlerActivity(MessageHandler messageHandler)
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
        }

        return activity;
    }
}