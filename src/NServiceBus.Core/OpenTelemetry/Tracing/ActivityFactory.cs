﻿namespace NServiceBus;

using System.Diagnostics;
using Pipeline;
using Sagas;
using Transport;

class ActivityFactory : IActivityFactory
{
    public Activity StartIncomingActivity(MessageContext context)
    {
        Activity activity;
        if (context.Extensions.TryGet(out Activity transportActivity) && transportActivity != null) // attach to transport span but link receive pipeline span to send pipeline span
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
            // TryParse doesn't have an overload that supports changing the isRemote setting yet
            // This can be removed with .NET 7, see https://github.com/dotnet/runtime/issues/42575
            var remoteParentActivityContext = new ActivityContext(sendSpanContext.TraceId, sendSpanContext.SpanId, sendSpanContext.TraceFlags, sendSpanContext.TraceState, isRemote: true);
            activity = ActivitySources.Main.CreateActivity(name: ActivityNames.IncomingMessageActivityName, ActivityKind.Consumer, remoteParentActivityContext);
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