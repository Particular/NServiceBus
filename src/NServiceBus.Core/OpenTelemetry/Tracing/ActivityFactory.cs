#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Extensibility;
using Logging;
using Pipeline;
using Transport;

sealed class ActivityFactory(InstrumentationOptions options) : IActivityFactory
{
    public InstrumentationOptions Options { get; } = options;

    static Activity? CreateActivityFromIncomingMessage(ActivitySource activitySource, string activityName, Dictionary<string, string> headers, string nativeMessageId, ContextBag extensions)
    {
        // CreateActivity is a no-op if there are no listeners but we are doing a fast path check
        // here nonetheless to avoid having to parse headers, access the extension bag, etc.
        if (!activitySource.HasListeners())
        {
            return null;
        }

        Activity? activity;
        var incomingTraceParentExists = headers.TryGetValue(Headers.DiagnosticsTraceParent, out var sendSpanId);
        var activityContextCreatedFromIncomingTraceParent = ActivityContext.TryParse(sendSpanId, null, out var sendSpanContext);

        if (extensions.TryGet<Activity>(out var transportActivity)) // attach to transport span but link receive pipeline span to send pipeline span
        {
            ActivityLink[]? links = null;
            if (incomingTraceParentExists && sendSpanId != transportActivity.Id)
            {
                if (activityContextCreatedFromIncomingTraceParent)
                {
                    links = [new ActivityLink(sendSpanContext)];
                }
            }

            activity = activitySource.CreateActivity(name: activityName,
                ActivityKind.Consumer, transportActivity.Context, links: links, idFormat: ActivityIdFormat.W3C);
        }
        else if (incomingTraceParentExists && activityContextCreatedFromIncomingTraceParent) // otherwise directly create child from logical send
        {
            var isStartNewTraceHeaderAvailable = headers.TryGetValue(Headers.StartNewTrace, out var shouldStartNewTrace);
            if (isStartNewTraceHeaderAvailable && shouldStartNewTrace?.Equals(bool.TrueString) is true)
            {
                // create a new trace or root activity
                ActivityLink[] links = [new(sendSpanContext)];
                //null the current activity so that the new one is created as root https://github.com/dotnet/runtime/issues/65528#issuecomment-2613486896
                Activity.Current = null;
                activity = activitySource.StartActivity(name: activityName, ActivityKind.Consumer, parentContext: default, tags: null, links: links);
            }
            else
            {
                // no new trace was requested, so start a child trace
                ActivityContext.TryParse(sendSpanId, null, true, out var remoteParentActivityContext);
                activity = activitySource.CreateActivity(name: activityName, ActivityKind.Consumer, remoteParentActivityContext);
            }
        }
        else // otherwise start a new trace
        {
            // This will set Activity.Current as parent if available
            activity = activitySource.CreateActivity(name: activityName, ActivityKind.Consumer);
        }

        if (activity is null)
        {
            return activity;
        }

        ContextPropagation.PropagateContextFromHeaders(activity, headers);

        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.AddTag(ActivityTags.NativeMessageId, nativeMessageId);

        ActivityDecorator.PromoteHeadersToTags(activity, headers);

        return activity;
    }

    public Activity? StartIncomingPipelineActivity(MessageContext context)
    {
        var activity = CreateActivityFromIncomingMessage(
            ActivitySources.Main,
            ActivityNames.IncomingMessageActivityName,
            context.Headers,
            context.NativeMessageId,
            context.Extensions);

        if (activity is null)
        {
            return activity;
        }

        activity.DisplayName = Options.UseMessageDestinationInSpanNames
            ? $"{ActivityDisplayNames.ProcessOperation} {context.ReceiveAddress}"
            : ActivityDisplayNames.ProcessMessage;

        activity.Start();

        return activity;
    }

    public Activity? StartOutgoingPipelineActivity(string activityName, string displayName, IBehaviorContext outgoingContext)
    {
        var activity = ActivitySources.Main.CreateActivity(activityName, ActivityKind.Producer);
        if (activity == null)
        {
            return activity;
        }

        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.DisplayName = displayName;
        activity.Start();

        outgoingContext.Extensions.SetOutgoingPipelineActivity(activity);

        return activity;
    }

    public Activity? StartHandlerActivity(MessageHandler messageHandler)
    {
        if (Activity.Current == null)
        {
            // don't call StartActivity if we haven't started an activity from the incoming pipeline to avoid the handlers being sampled although the incoming message isn't.
            return null;
        }

        // Until v11 the dedicated handler source is opt-in; existing configurations only
        // subscribe to the main source and must keep receiving handler spans from it.
        var source = HandlerActivitySourceSwitch.UseHandlerActivitySource
            ? ActivitySources.Handler
            : ActivitySources.Main;

        var activity = source.StartActivity(ActivityNames.InvokeHandlerActivityName);

        if (activity is null)
        {
            return activity;
        }

        activity.DisplayName = messageHandler.HandlerType.Name;
        activity.AddTag(ActivityTags.HandlerType, messageHandler.HandlerType.FullName);
        return activity;
    }

    public Activity? StartRecoverabilityActivity(ErrorContext context)
    {
        var activity = CreateActivityFromIncomingMessage(
            ActivitySources.Recoverability,
            ActivityNames.RecoverabilityActivityName,
            context.Headers,
            context.NativeMessageId,
            context.Extensions);

        if (activity is null)
        {
            return activity;
        }

        activity.DisplayName = ActivityDisplayNames.Recoverability;

        activity.Start();

        return activity;
    }

    public void UpdateActivityFromRecoverabilityAction(Activity activity, RecoverabilityAction recoverabilityAction, string receiveAddress)
    {
        if (recoverabilityAction is ImmediateRetry)
        {
            activity.AddTag(ActivityTags.RecoverabilityAction, "immediate_retry");
            activity.DisplayName = ActivityDisplayNames.ImmediateRetryOperation;

            if (Options.UseMessageDestinationInSpanNames)
            {
                activity.DisplayName += $" {receiveAddress}";
            }
        }
        else if (recoverabilityAction is DelayedRetry)
        {
            activity.AddTag(ActivityTags.RecoverabilityAction, "delayed_retry");
            activity.DisplayName = ActivityDisplayNames.DelayedRetryOperation;

            if (Options.UseMessageDestinationInSpanNames)
            {
                activity.DisplayName += $" {receiveAddress}";
            }
        }
        else if (recoverabilityAction is MoveToError moveToError)
        {
            activity.AddTag(ActivityTags.RecoverabilityAction, "move_to_error");

            activity.DisplayName = Options.UseMessageDestinationInSpanNames
                ? $"{ActivityDisplayNames.MoveToErrorOperation} {moveToError.ErrorQueue}"
                : $"{ActivityDisplayNames.MoveToErrorOperation} error";
        }
        else if (recoverabilityAction is Discard)
        {
            activity.AddTag(ActivityTags.RecoverabilityAction, "discard");
            activity.DisplayName = ActivityDisplayNames.DiscardOperation;
        }
    }

    public void RecordError(Activity activity, Exception exception, ContextBag context)
    {
        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity.SetTag(ActivityTags.ErrorType, exception.GetType().FullName);

        LegacyExceptionTags.SetLegacyStatusTags(activity, exception);

        if (!exception.Data.Contains(ActivityTags.ExceptionRecordedFlag))
        {
            if (Options.ExceptionRecordingMode == ExceptionRecordingMode.Logs)
            {
                Logger.Error($"An exception occurred while executing '{activity.DisplayName}'.", exception);
            }
            else
            {
                activity.AddException(exception, LegacyExceptionTags.EscapedTagList);
            }

            exception.Data.Add(ActivityTags.ExceptionRecordedFlag, true);
        }

        if (exception is TaskCanceledException)
        {
            activity.SetTag(ActivityTags.CancelledTask, true);
        }
    }

    static readonly ILog Logger = LogManager.GetLogger<ActivityFactory>();
}